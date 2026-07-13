using GzsBilling.Domain.Enums;

namespace GzsBilling.Domain.Entities;

/// <summary>
/// Represents a billing dispute raised against a transaction.
/// </summary>
public class Dispute
{
    public Guid Id { get; set; }
    public string DisputeId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string ContragentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset SlaDeadline { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public List<DisputeHistoryEntry> History { get; set; } = new();
    public List<DisputeEvidence> Evidence { get; set; } = new();
}
