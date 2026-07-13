namespace GzsBilling.Domain.Events;

public class WebhookReceivedEvent
{
    public Guid CorrelationId { get; set; }
    public string ContragentId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
}
