namespace GzsBilling.Domain.Events;

public class PaymentProcessedEvent
{
    public Guid CorrelationId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string ContragentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UZS";
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
