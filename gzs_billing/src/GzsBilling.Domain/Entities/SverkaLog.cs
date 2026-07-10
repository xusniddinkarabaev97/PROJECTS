using GzsBilling.Domain.Enums;

namespace GzsBilling.Domain.Entities;

public class SverkaLog
{
    public Guid Id { get; set; }
    public DateOnly ReconciliationDate { get; set; }
    public SverkaIssueType IssueType { get; set; }
    public string Details { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
