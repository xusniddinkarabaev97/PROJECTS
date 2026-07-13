using System;

namespace GzsBilling.Infrastructure.Outbox;

/// <summary>
/// Represents a message stored in the transactional outbox for reliable publishing.
/// </summary>
public class OutboxMessage
{
    /// <summary>Unique identifier for the outbox record.</summary>
    public Guid Id { get; set; }

    /// <summary>Fully-qualified CLR type name or event name used for deserialization.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Serialized event payload in JSON format.</summary>
    public string EventPayload { get; set; } = string.Empty;

    /// <summary>Timestamp when the message was first persisted.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Timestamp when the message was successfully published, or null if still pending.</summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>Current lifecycle status of the outbox message.</summary>
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    /// <summary>Number of publish attempts that have been made so far.</summary>
    public int RetryCount { get; set; }

    /// <summary>Error message captured from the last failed publish attempt, if any.</summary>
    public string? Error { get; set; }
}

/// <summary>
/// Lifecycle states for an outbox message.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>Waiting to be picked up by the publisher.</summary>
    Pending = 0,

    /// <summary>Currently being processed by a publisher instance.</summary>
    Processing = 1,

    /// <summary>Successfully published to the message broker.</summary>
    Published = 2,

    /// <summary>Publishing failed after all retries were exhausted.</summary>
    Failed = 3
}
