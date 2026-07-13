using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GzsBilling.Infrastructure.Outbox;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IOutboxStore"/>.
/// Intended for development and testing only.
/// Production deployments must replace this with an EF Core-backed store
/// that participates in the same database transaction as the business operation.
/// </summary>
public sealed class OutboxStore : IOutboxStore
{
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _messages = new();

    /// <inheritdoc />
    public Task SaveAsync<T>(T @event, string eventType)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            EventPayload = JsonSerializer.Serialize(@event),
            CreatedAt = DateTimeOffset.UtcNow,
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0
        };

        _messages.TryAdd(message.Id, message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize)
    {
        var pending = _messages.Values
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<OutboxMessage>>(pending);
    }

    /// <inheritdoc />
    public Task MarkAsPublishedAsync(Guid messageId)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.Status = OutboxMessageStatus.Published;
            message.ProcessedAt = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkAsFailedAsync(Guid messageId, string error)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.Status = OutboxMessageStatus.Failed;
            message.Error = error;
            message.ProcessedAt = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }
}
