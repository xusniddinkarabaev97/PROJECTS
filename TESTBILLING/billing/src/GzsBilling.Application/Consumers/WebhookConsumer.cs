using MassTransit;
using Microsoft.Extensions.Logging;
using GzsBilling.Domain.Events;

namespace GzsBilling.Application.Consumers;

public class WebhookConsumer : IConsumer<WebhookReceivedEvent>
{
    private readonly ILogger<WebhookConsumer> _logger;

    public WebhookConsumer(ILogger<WebhookConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WebhookReceivedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing webhook: CorrId={CorrId}, Contragent={Contragent}, Event={Event}, TxnId={TxnId}",
            message.CorrelationId, message.ContragentId, message.EventType, message.TransactionId);

        try
        {
            // Process webhook based on event type
            switch (message.EventType)
            {
                case "payment_status":
                    await ProcessPaymentStatus(message, context);
                    break;
                case "reconciliation":
                    await ProcessReconciliation(message, context);
                    break;
                default:
                    _logger.LogWarning("Unknown webhook event type: {EventType}", message.EventType);
                    break;
            }

            // Acknowledge successful processing
            _logger.LogInformation(
                "Webhook processed successfully: CorrId={CorrId}", message.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process webhook: CorrId={CorrId}, Event={Event}",
                message.CorrelationId, message.EventType);

            // NOT acknowledged - will be retried according to retry policy
            throw;
        }
    }

    private Task ProcessPaymentStatus(WebhookReceivedEvent message, ConsumeContext context)
    {
        _logger.LogInformation(
            "Processing payment status update: TxnId={TxnId}, Contragent={Contragent}",
            message.TransactionId, message.ContragentId);

        // Here you would update transaction status in the database
        // and publish PaymentProcessedEvent if needed

        return Task.CompletedTask;
    }

    private Task ProcessReconciliation(WebhookReceivedEvent message, ConsumeContext context)
    {
        _logger.LogInformation(
            "Processing reconciliation data from {Contragent}", message.ContragentId);

        // Here you would process reconciliation data
        // Compare with internal records (3-way reconciliation)

        return Task.CompletedTask;
    }
}
