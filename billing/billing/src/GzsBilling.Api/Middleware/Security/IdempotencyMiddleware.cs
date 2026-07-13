using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GzsBilling.Api.Middleware.Security;

/// <summary>
/// Middleware that enforces idempotent request processing using the X-Idempotency-Key header.
/// Caches the original response for 24 hours and replays it on duplicate keys.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private const string IdempotencyHeader = "X-Idempotency-Key";
    private const string ReplayedHeader = "X-Idempotent-Replayed";
    private const string CacheKeyPrefix = "idem:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Idempotency only applies to mutating requests.
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method) ||
            HttpMethods.IsTrace(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers[IdempotencyHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _logger.LogWarning(
                "Idempotency: Missing '{HeaderName}' header. Path={Path} Method={Method}",
                IdempotencyHeader, context.Request.Path, context.Request.Method);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                "{\"error\":\"Missing required header 'X-Idempotency-Key'.\"}");
            return;
        }

        // Validate UUID v4 format using the "D" format specifier (32 digits, 4 hyphens).
        if (!Guid.TryParseExact(idempotencyKey, "D", out _))
        {
            _logger.LogWarning(
                "Idempotency: Invalid UUID v4 format for key '{Key}'. Path={Path} Method={Method}",
                idempotencyKey, context.Request.Path, context.Request.Method);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                "{\"error\":\"Invalid 'X-Idempotency-Key' format. Expected UUID v4 (e.g. 550e8400-e29b-41d4-a716-446655440000).\"}");
            return;
        }

        // Derive the cache key: SHA-256 of the idempotency key, first 16 hex characters.
        var cacheKey = DeriveCacheKey(idempotencyKey);

        // Check if a cached response already exists.
        string? cachedResponse;
        try
        {
            cachedResponse = await _cache.GetStringAsync(cacheKey);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex,
                "Idempotency: Redis unavailable for key '{CacheKey}'. Returning 503.", cacheKey);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                "{\"error\":\"Service temporarily unavailable. Unable to verify idempotency.\"}");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Idempotency: Unexpected error accessing Redis for key '{CacheKey}'. Returning 503.", cacheKey);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                "{\"error\":\"Service temporarily unavailable. Unable to verify idempotency.\"}");
            return;
        }

        if (cachedResponse is not null)
        {
            // Replay the cached response.
            var cached = JsonSerializer.Deserialize<CachedResponse>(cachedResponse);
            if (cached is not null)
            {
                _logger.LogInformation(
                    "Idempotency: Replaying cached response for key '{CacheKey}'. Original status: {StatusCode}",
                    cacheKey, cached.StatusCode);

                context.Response.StatusCode = cached.StatusCode;
                context.Response.ContentType = cached.ContentType;
                context.Response.Headers[ReplayedHeader] = "true";

                if (cached.BodyBase64 is not null)
                {
                    var bodyBytes = Convert.FromBase64String(cached.BodyBase64);
                    await context.Response.Body.WriteAsync(bodyBytes);
                }

                return;
            }
        }

        // Enable request body buffering so we can read it multiple times.
        context.Request.EnableBuffering();

        // Capture the response by replacing the response body stream.
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            // Read the captured response body.
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBodyBytes = await ReadStreamAsync(responseBodyStream);

            // Cache the response for future idempotent replays.
            var cached = new CachedResponse
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType ?? "application/octet-stream",
                BodyBase64 = responseBodyBytes.Length > 0
                    ? Convert.ToBase64String(responseBodyBytes)
                    : null
            };

            try
            {
                var serialized = JsonSerializer.Serialize(cached);
                await _cache.SetStringAsync(
                    cacheKey,
                    serialized,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = CacheTtl
                    });

                _logger.LogInformation(
                    "Idempotency: Cached response for key '{CacheKey}' (status {StatusCode}, {BodyLen} bytes, TTL 24h)",
                    cacheKey, cached.StatusCode, responseBodyBytes.Length);
            }
            catch (RedisConnectionException ex)
            {
                // Failed to cache, but the response was already generated.
                // Log the error and continue – the client still gets their response.
                _logger.LogError(ex,
                    "Idempotency: Failed to cache response for key '{CacheKey}'. Response still returned to client.",
                    cacheKey);
            }

            // Copy the captured response back to the original stream.
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Derives a deterministic, compact cache key from the idempotency key.
    /// Uses SHA-256 and takes the first 16 hex characters, prefixed with "idem:".
    /// </summary>
    private static string DeriveCacheKey(string idempotencyKey)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey));
        var hex = Convert.ToHexString(hashBytes);
        return CacheKeyPrefix + hex[..16];
    }

    private static async Task<byte[]> ReadStreamAsync(Stream stream)
    {
        if (stream is MemoryStream ms)
        {
            return ms.ToArray();
        }

        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        return copy.ToArray();
    }

    /// <summary>
    /// Serializable representation of a cached HTTP response.
    /// </summary>
    private sealed class CachedResponse
    {
        public int StatusCode { get; set; }
        public string ContentType { get; set; } = "application/json";
        public string? BodyBase64 { get; set; }
    }
}
