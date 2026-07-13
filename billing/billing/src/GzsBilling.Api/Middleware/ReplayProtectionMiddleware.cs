using System.Globalization;
using GzsBilling.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GzsBilling.Api.Middleware;

public class ReplayProtectionMiddleware
{
    private static readonly HashSet<string> SkipMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options
    };

    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ReplayProtectionMiddleware> _logger;
    private readonly ReplayProtectionSettings _settings;

    public ReplayProtectionMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        IOptions<ReplayProtectionSettings> options,
        ILogger<ReplayProtectionMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _settings = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (SkipMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/v1/payments/test-imitation", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/v1/stations", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/v1/users", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/v1/shareholders", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/v1/refunds", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/v1/disputes", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/v1/reconciliation", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/v1/payments", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/pay", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/qr", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/ugaz", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/pay", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var nonce = context.Request.Headers[_settings.NonceHeaderName].FirstOrDefault();
        var timestampHeader = context.Request.Headers[_settings.TimestampHeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(nonce))
        {
            _logger.LogWarning(
                "ReplayProtection: Missing header '{HeaderName}'. RemoteIp={RemoteIp} Path={Path}",
                _settings.NonceHeaderName,
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                $"{{\"error\":\"Missing required header '{_settings.NonceHeaderName}'\"}}");
            return;
        }

        if (string.IsNullOrWhiteSpace(timestampHeader))
        {
            _logger.LogWarning(
                "ReplayProtection: Missing header '{HeaderName}'. RemoteIp={RemoteIp} Path={Path} Nonce={Nonce}",
                _settings.TimestampHeaderName,
                context.Connection.RemoteIpAddress,
                context.Request.Path,
                nonce);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                $"{{\"error\":\"Missing required header '{_settings.TimestampHeaderName}'\"}}");
            return;
        }

        if (!long.TryParse(timestampHeader, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixTimestampSeconds))
        {
            _logger.LogWarning(
                "ReplayProtection: Invalid timestamp format '{Timestamp}'. RemoteIp={RemoteIp} Path={Path} Nonce={Nonce}",
                timestampHeader,
                context.Connection.RemoteIpAddress,
                context.Request.Path,
                nonce);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                "{\"error\":\"Invalid timestamp format. Expected Unix timestamp in seconds.\"}");
            return;
        }

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestampSeconds);
        var currentTime = DateTimeOffset.UtcNow;
        var maxAge = TimeSpan.FromMinutes(_settings.MaxAgeMinutes);
        var age = currentTime - requestTime;

        if (Math.Abs(age.TotalMinutes) > _settings.MaxAgeMinutes)
        {
            _logger.LogWarning(
                "ReplayProtection: Request expired. Age={Age:F1}min MaxAge={MaxAge}min. RemoteIp={RemoteIp} Path={Path} Nonce={Nonce}",
                Math.Abs(age.TotalMinutes),
                _settings.MaxAgeMinutes,
                context.Connection.RemoteIpAddress,
                context.Request.Path,
                nonce);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                "{\"error\":\"Request expired. Timestamp is outside the allowed window.\"}");
            return;
        }

        var cacheKey = _settings.CachePrefix + nonce;

        var existingNonce = await _cache.GetStringAsync(cacheKey);
        if (existingNonce != null)
        {
            _logger.LogWarning(
                "ReplayProtection: Replay attack detected. Nonce={Nonce} RemoteIp={RemoteIp} Path={Path}",
                nonce,
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                "{\"error\":\"Replay attack detected.\"}");
            return;
        }

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_settings.MaxAgeMinutes)
        };

        await _cache.SetStringAsync(cacheKey, currentTime.ToString("o", CultureInfo.InvariantCulture), cacheOptions);

        await _next(context);
    }
}
