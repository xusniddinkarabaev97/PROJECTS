using GzsBilling.Domain.Enums;

namespace GzsBilling.Domain.Entities;

/// <summary>
/// Immutable record of a status change or action taken on a dispute.
/// </summary>
public class DisputeHistoryEntry
{
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DisputeStatus? PreviousStatus { get; set; }
    public DisputeStatus? NewStatus { get; set; }
}
