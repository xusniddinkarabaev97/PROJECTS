namespace GzsBilling.Domain.Entities;

public class ReconciliationReport
{
    public Guid Id { get; set; }
    public DateTimeOffset ReportDate { get; set; } // Date of reconciliation
    public string Provider { get; set; } = string.Empty; // uzcard, humo, etc.
    public ReconciliationStatus Status { get; set; }
    public int BillingTransactionCount { get; set; }
    public decimal BillingTotalAmount { get; set; }
    public int ProviderTransactionCount { get; set; }
    public decimal ProviderTotalAmount { get; set; }
    public int DiscrepancyCount { get; set; }
    public decimal DiscrepancyAmount { get; set; }
    public decimal DiscrepancyPercentage { get; set; }
    public decimal ThresholdPercentage { get; set; } = 0.01m; // 0.01%
    public bool ThresholdExceeded { get; set; }
    public string? DiscrepancyDetails { get; set; } // JSON array of discrepancies
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
}

public enum ReconciliationStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    DiscrepanciesFound = 3,
    Reviewed = 4,
    Failed = 5
}

public class DiscrepancyItem
{
    public string TransactionId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // MissingInBilling, MissingInProvider, AmountMismatch
    public decimal BillingAmount { get; set; }
    public decimal ProviderAmount { get; set; }
    public decimal Difference { get; set; }
    public string? Notes { get; set; }
}
