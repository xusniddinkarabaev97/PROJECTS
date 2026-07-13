namespace GzsBilling.Infrastructure.Configuration;

public class WebhookSettings
{
    public string SignatureHeaderName { get; set; } = "X-Signature-256";
    public string SignatureAlgorithm { get; set; } = "HMACSHA256";
    public string TimestampHeaderName { get; set; } = "X-Webhook-Timestamp";
    public int ToleranceSeconds { get; set; } = 300;
    public Dictionary<string, string> ContragentSecrets { get; set; } = new();
}
