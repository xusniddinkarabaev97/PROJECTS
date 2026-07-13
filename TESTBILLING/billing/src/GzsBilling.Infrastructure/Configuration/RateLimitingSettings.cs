namespace GzsBilling.Infrastructure.Configuration;

public class RateLimitingSettings
{
    public RateLimitPolicyConfig DefaultPolicy { get; set; } = new();
    public Dictionary<string, RateLimitPolicyConfig> ContragentPolicies { get; set; } = new();
}

public class RateLimitPolicyConfig
{
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
    public string QueueProcessingOrder { get; set; } = "OldestFirst";
    public int QueueLimit { get; set; }
}
