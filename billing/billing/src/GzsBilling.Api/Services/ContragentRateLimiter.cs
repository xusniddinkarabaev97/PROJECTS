using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using GzsBilling.Infrastructure.Configuration;

namespace GzsBilling.Api.Services;

public interface IContragentRateLimiter
{
    string GetPolicyName(string contragentId);
    RateLimitPartition<string> GetPartition(HttpContext context);
}

public class ContragentRateLimiter : IContragentRateLimiter
{
    private readonly RateLimitingSettings _settings;

    public ContragentRateLimiter(IOptions<RateLimitingSettings> options)
    {
        _settings = options.Value;
    }

    public string GetPolicyName(string contragentId)
    {
        if (_settings.ContragentPolicies.ContainsKey(contragentId))
            return contragentId;
        return "Default";
    }

    public RateLimitPartition<string> GetPartition(HttpContext context)
    {
        // Extract contragent ID from token claims or API key header
        var contragentId = context.User?.FindFirst("contragent_id")?.Value
                          ?? context.Request.Headers["X-API-Key"].FirstOrDefault()
                          ?? context.Connection.RemoteIpAddress?.ToString()
                          ?? "unknown";

        var policy = _settings.ContragentPolicies.GetValueOrDefault(contragentId, _settings.DefaultPolicy);

        var order = policy.QueueProcessingOrder == "NewestFirst"
            ? QueueProcessingOrder.NewestFirst
            : QueueProcessingOrder.OldestFirst;

        return RateLimitPartition.GetFixedWindowLimiter(contragentId, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromSeconds(policy.WindowSeconds),
                QueueProcessingOrder = order,
                QueueLimit = policy.QueueLimit,
                AutoReplenishment = true
            });
    }
}
