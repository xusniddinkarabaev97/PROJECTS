namespace GzsBilling.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string ContragentId { get; set; } = string.Empty;
    public string PaymentSystem { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UZS";
    public TransactionStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public Guid? StationId { get; set; }
    public Guid? ColumnId { get; set; }
    public string? StationName { get; set; }
    public string? ColumnName { get; set; }
}

public enum TransactionStatus
{
    Created = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4,
    Reconciled = 5
}
