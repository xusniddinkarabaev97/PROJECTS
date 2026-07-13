using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GzsBilling.Infrastructure.Outbox;

/// <summary>
/// Background service that periodically polls the outbox store for pending messages
/// and publishes them to the message broker via MassTransit.
/// </summary>
public sealed class OutboxPublisher : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxRetries = 3;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    private readonly IOutboxStore _store;
    private readonly IBus _bus;
    private readonly ILogger<OutboxPublisher> _logger;

    // In-memory DLQ for messages that have exhausted all retry attempts.
    // In production this would be a dedicated database table (e.g. outbox_failed).
    private readonly ConcurrentBag<OutboxMessage> _deadLetterQueue = new();

    public OutboxPublisher(
        IOutboxStore store,
        IBus bus,
        ILogger<OutboxPublisher> logger)
    {
        _store = store;
        _bus = bus;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher started. Polling every {IntervalMs}ms, batch size {BatchSize}",
            PollingInterval.TotalMilliseconds, BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in OutboxPublisher processing loop");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxPublisher stopped. DLQ count: {DlqCount}", _deadLetterQueue.Count);
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        var pending = await _store.GetPendingAsync(BatchSize);

        if (pending.Count == 0)
        {
            return;
        }

        _logger.LogDebug("OutboxPublisher picked up {Count} pending messages", pending.Count);

        foreach (var message in pending)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            await ProcessMessageAsync(message, ct);
        }
    }

    private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug(
                "Publishing outbox message {MessageId} of type {EventType} (attempt {Attempt})",
                message.Id, message.EventType, message.RetryCount + 1);

            // Deserialize the event payload and publish to MassTransit.
            // The EventType is used to resolve the correct CLR type for deserialization.
            var eventType = Type.GetType(message.EventType);
            if (eventType is null)
            {
                await HandleFailureAsync(
                    message,
                    $"Cannot resolve event type '{message.EventType}'. Ensure the assembly is loaded.",
                    ct);
                return;
            }

            var eventPayload = JsonSerializer.Deserialize(message.EventPayload, eventType);
            if (eventPayload is null)
            {
                await HandleFailureAsync(
                    message,
                    $"Failed to deserialize payload for event type '{message.EventType}'.",
                    ct);
                return;
            }

            await _bus.Publish(eventPayload, eventType, ct);

            await _store.MarkAsPublishedAsync(message.Id);

            _logger.LogInformation(
                "Outbox message {MessageId} published successfully (type {EventType})",
                message.Id, message.EventType);
        }
        catch (Exception ex)
        {
            await HandleFailureAsync(message, ex.Message, ct);
        }
    }

    private async Task HandleFailureAsync(OutboxMessage message, string error, CancellationToken ct)
    {
        message.RetryCount++;
        message.Error = error;

        if (message.RetryCount >= MaxRetries)
        {
            // Exhausted all retries – move to DLQ.
            _logger.LogError(
                "Outbox message {MessageId} exhausted all {MaxRetries} retries. Moving to DLQ. " +
                "EventType: {EventType}, Error: {Error}",
                message.Id, MaxRetries, message.EventType, error);

            await _store.MarkAsFailedAsync(message.Id, error);
            _deadLetterQueue.Add(message);
        }
        else
        {
            // Exponential backoff before next retry.
            var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, message.RetryCount));

            _logger.LogWarning(
                "Outbox message {MessageId} publish failed (attempt {Attempt}/{MaxRetries}). " +
                "Retrying after {DelayMs:F0}ms. Error: {Error}",
                message.Id, message.RetryCount, MaxRetries, delay.TotalMilliseconds, error);

            await Task.Delay(delay, ct);

            // The message remains in Pending state so it will be picked up again
            // on the next polling cycle.
        }
    }
}
