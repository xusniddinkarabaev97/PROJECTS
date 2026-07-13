using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GzsBilling.Domain.Events;
using MassTransit;
using System.Text.Json;

namespace GzsBilling.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous] // Auth handled by WebhookSignatureMiddleware
public class WebhooksController : ControllerBase
{
    private readonly ILogger<WebhooksController> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public WebhooksController(ILogger<WebhooksController> logger, IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Receive payment status callback from external payment systems
    /// </summary>
    [HttpPost("{contragentId}/payment-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PaymentStatusCallback(
        string contragentId,
        [FromBody] JsonElement payload)
    {
        var rawPayload = payload.GetRawText();

        _logger.LogInformation(
            "Webhook received: Contragent={ContragentId}, Event=payment_status",
            contragentId);

        // Publish event to message queue for async processing
        var webhookEvent = new WebhookReceivedEvent
        {
            CorrelationId = Guid.NewGuid(),
            ContragentId = contragentId,
            EventType = "payment_status",
            TransactionId = payload.TryGetProperty("transaction_id", out var txnId)
                ? txnId.GetString() ?? string.Empty
                : string.Empty,
            RawPayload = rawPayload,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        await _publishEndpoint.Publish(webhookEvent);

        return Ok(new { status = "accepted", correlation_id = webhookEvent.CorrelationId });
    }

    /// <summary>
    /// Receive reconciliation callback
    /// </summary>
    [HttpPost("{contragentId}/reconciliation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReconciliationCallback(
        string contragentId,
        [FromBody] JsonElement payload)
    {
        var rawPayload = payload.GetRawText();

        _logger.LogInformation(
            "Webhook received: Contragent={ContragentId}, Event=reconciliation",
            contragentId);

        var webhookEvent = new WebhookReceivedEvent
        {
            CorrelationId = Guid.NewGuid(),
            ContragentId = contragentId,
            EventType = "reconciliation",
            TransactionId = string.Empty,
            RawPayload = rawPayload,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        await _publishEndpoint.Publish(webhookEvent);

        return Ok(new { status = "accepted", correlation_id = webhookEvent.CorrelationId });
    }
}
