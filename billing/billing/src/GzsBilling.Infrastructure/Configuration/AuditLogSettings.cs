namespace GzsBilling.Infrastructure.Configuration;

public class AuditLogSettings
{
    public string LogDirectory { get; set; } = "logs/audit";
    public int RetentionDays { get; set; } = 365;
    public bool EncryptLogs { get; set; } = true;
    public string EncryptionKey { get; set; } = string.Empty;
    public string LogLevel { get; set; } = "Information";
    public bool IncludeRequestBody { get; set; }
    public bool IncludeResponseBody { get; set; }
    public int MaxRequestBodyLogSize { get; set; } = 4096;
}
