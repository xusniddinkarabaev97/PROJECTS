namespace GzsBilling.Infrastructure.Configuration;

public class ReplayProtectionSettings
{
    public string NonceHeaderName { get; set; } = "X-Nonce";
    public string TimestampHeaderName { get; set; } = "X-Timestamp";
    public int MaxAgeMinutes { get; set; } = 5;
    public string CachePrefix { get; set; } = "replay:nonce:";
}
