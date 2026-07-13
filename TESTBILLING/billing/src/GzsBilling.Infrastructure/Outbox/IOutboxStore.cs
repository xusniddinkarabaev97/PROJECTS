using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GzsBilling.Infrastructure.Outbox;

/// <summary>
/// Abstraction over the transactional outbox storage.
/// Implementations persist domain events atomically within the same database transaction
/// so that the outbox publisher can later forward them to the message broker.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Serializes <paramref name="event"/> to JSON and persists it as a new
    /// <see cref="OutboxMessage"/> with the given <paramref name="eventType"/>.
    /// In a real EF Core implementation this happens inside the ambient DbContext transaction.
    /// </summary>
    /// <typeparam name="T">The domain event type.</typeparam>
    /// <param name="event">The domain event instance to persist.</param>
    /// <param name="eventType">
    /// The event type identifier (e.g. the fully-qualified CLR type name or a custom string).
    /// </param>
    Task SaveAsync<T>(T @event, string eventType);

    /// <summary>
    /// Returns up to <paramref name="batchSize"/> messages that are still in
    /// <see cref="OutboxMessageStatus.Pending"/> state, ordered by creation time ascending.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize);

    /// <summary>
    /// Marks the specified message as <see cref="OutboxMessageStatus.Published"/>
    /// and records the processing timestamp.
    /// </summary>
    Task MarkAsPublishedAsync(Guid messageId);

    /// <summary>
    /// Marks the specified message as <see cref="OutboxMessageStatus.Failed"/>
    /// and records the error message.
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error);
}
