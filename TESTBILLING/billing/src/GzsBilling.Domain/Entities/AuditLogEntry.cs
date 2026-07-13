namespace GzsBilling.Domain.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public long DurationMs { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string SourceIp { get; set; } = string.Empty;
}
