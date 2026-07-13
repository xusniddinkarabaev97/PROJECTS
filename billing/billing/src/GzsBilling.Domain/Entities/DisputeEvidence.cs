namespace GzsBilling.Domain.Entities;

/// <summary>
/// Evidence document attached to a dispute.
/// </summary>
public class DisputeEvidence
{
    public Guid Id { get; set; }
    public string EvidenceId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
}
