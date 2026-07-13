using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GzsBilling.Infrastructure.Configuration;

namespace GzsBilling.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ReplayProtectionAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
        var settings = context.HttpContext.RequestServices.GetRequiredService<IOptions<ReplayProtectionSettings>>().Value;
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ReplayProtectionAttribute>>();
        var request = context.HttpContext.Request;

        // Skip for GET/HEAD/OPTIONS
        if (request.Method is "GET" or "HEAD" or "OPTIONS")
        {
            await next();
            return;
        }

        var nonce = request.Headers[settings.NonceHeaderName].FirstOrDefault();
        var timestampStr = request.Headers[settings.TimestampHeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(nonce) || string.IsNullOrEmpty(timestampStr))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "replay_protection_required",
                message = "X-Nonce and X-Timestamp headers are required"
            });
            return;
        }

        if (!long.TryParse(timestampStr, out var unixTimestamp))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "invalid_timestamp",
                message = "X-Timestamp must be a valid Unix timestamp (seconds since epoch)"
            });
            return;
        }

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        var age = DateTimeOffset.UtcNow - requestTime;

        if (Math.Abs(age.TotalMinutes) > settings.MaxAgeMinutes)
        {
            logger.LogWarning("Replay protection: Request expired. Age: {Age:F1} min, Max: {Max} min",
                age.TotalMinutes, settings.MaxAgeMinutes);

            context.Result = new UnauthorizedObjectResult(new
            {
                error = "request_expired",
                message = $"Request timestamp is outside the allowed window of {settings.MaxAgeMinutes} minutes"
            });
            return;
        }

        var cacheKey = $"{settings.CachePrefix}{nonce}";
        var existing = await cache.GetStringAsync(cacheKey);

        if (existing != null)
        {
            logger.LogWarning("Replay protection: Nonce {Nonce} already used (replay attack detected)", nonce);

            context.Result = new UnauthorizedObjectResult(new
            {
                error = "replay_detected",
                message = "This request has already been processed (replay attack detected)"
            });
            return;
        }

        await cache.SetStringAsync(cacheKey, "used", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(settings.MaxAgeMinutes + 1)
        });

        await next();
    }
}
