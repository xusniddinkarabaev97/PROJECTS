using MassTransit;
using Microsoft.Extensions.Logging;
using GzsBilling.Domain.Events;

namespace GzsBilling.Application.Consumers;

public class PaymentEventConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly ILogger<PaymentEventConsumer> _logger;

    public PaymentEventConsumer(ILogger<PaymentEventConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing payment event: CorrId={CorrId}, TxnId={TxnId}, Amount={Amount} {Currency}, Status={Status}",
            message.CorrelationId, message.TransactionId, message.Amount,
            message.Currency, message.Status);

        try
        {
            // Update transaction in database
            // Notify contragent
            // Trigger reconciliation

            _logger.LogInformation(
                "Payment event processed: TxnId={TxnId}", message.TransactionId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process payment event: TxnId={TxnId}", message.TransactionId);
            await Task.CompletedTask;
            throw;
        }
    }
}
