using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Stores a message for another service in the same transaction as the business change that causes it.
/// A background processor publishes the pending rows, so a message leaves the service only when its business
/// change commits.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Creates a pending message that the processor can publish at once.
    /// </summary>
    /// <param name="type">The stable contract name from <see cref="IIntegrationMessage.MessageType"/>.</param>
    /// <param name="payload">The message serialized as JSON.</param>
    /// <param name="occurredOn">The UTC time of the business change.</param>
    /// <param name="traceParent">
    /// The trace context of the operation that created the message, or <see langword="null"/> without an active
    /// trace.
    /// </param>
    public OutboxMessage(string type, string payload, DateTime occurredOn, string? traceParent)
    {
        Type = type;
        Payload = payload;
        OccurredOn = occurredOn;
        NextAttemptOn = occurredOn;
        TraceParent = traceParent;
    }

    /// <summary>
    /// The application generates the identifier. The processor sends it as the broker message ID, so a receiver
    /// can drop a duplicate delivery.
    /// </summary>
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTime OccurredOn { get; private set; }

    /// <summary>The earliest UTC time of the next publish attempt. The processor moves it after a failure.</summary>
    public DateTime NextAttemptOn { get; private set; }

    /// <summary>The UTC time of the successful publish, or <see langword="null"/> while the message waits.</summary>
    public DateTime? ProcessedOn { get; private set; }

    public int AttemptCount { get; private set; }

    public string? Error { get; private set; }

    public string? TraceParent { get; private set; }
}
