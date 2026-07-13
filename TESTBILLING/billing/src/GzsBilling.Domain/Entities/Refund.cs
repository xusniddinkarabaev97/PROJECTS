namespace GzsBilling.Domain.Entities;

public class Refund
{
    public Guid Id { get; set; }
    public string RefundId { get; set; } = string.Empty; // REF-YYYYMMDD-XXXXXX
    public string OriginalTransactionId { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = "UZS";
    public string InitiatorId { get; set; } = string.Empty;
    public string InitiatorRole { get; set; } = string.Empty; // admin, customer, system
    public string Reason { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.Initiated;
    public string ProviderRefundId { get; set; } = string.Empty;
    public string? ApprovalChain { get; set; } // JSON
    public List<RefundStatusHistory> StatusHistory { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? SlaDeadline { get; set; } // 24 hours from initiation
    public bool FraudCheckPassed { get; set; }
    public string? FraudCheckNotes { get; set; }
}

public enum RefundStatus
{
    Initiated = 0,
    FraudCheckPending = 1,
    Approved = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5,
    Rejected = 6
}

public class RefundStatusHistory
{
    public Guid Id { get; set; }
    public RefundStatus Status { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
