using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.Intake;

/// <summary>
/// A stored raw MQTT message with its errors. The row is append-only: it has no audit fields and no soft delete. The
/// unique index on the machine and the sampling time drops a repeated delivery.
/// </summary>
public class ReceivedMessage : IEntity
{
    /// <summary>
    /// Creates a received message.
    /// </summary>
    /// <param name="machineRegistrationId">The machine of the topic, or <see langword="null"/> for an unknown
    /// gateway.</param>
    /// <param name="topic">The topic of the message.</param>
    /// <param name="sampledAt">The sampling time of the payload, or <see langword="null"/> when it is
    /// unreadable.</param>
    /// <param name="receivedAt">The moment of receipt, as a UTC instant.</param>
    /// <param name="schemaRevision">The schema revision that validated the message, or <see langword="null"/> when no
    /// schema applied.</param>
    /// <param name="rawPayload">The bytes of the payload, only when the database cannot store them as
    /// <c>jsonb</c> (malformed or unstorable JSON, a processing failure); otherwise <see langword="null"/>.</param>
    /// <param name="payload">The payload as JSON text, or <see langword="null"/> when the bytes are not JSON that the
    /// database can store.</param>
    /// <param name="errors">The errors of the message.</param>
    /// <exception cref="InvalidOperationException">Both or neither of <paramref name="rawPayload"/> and
    /// <paramref name="payload"/> are set.</exception>
    public ReceivedMessage(
        Guid? machineRegistrationId,
        string topic,
        DateTime? sampledAt,
        DateTime receivedAt,
        int? schemaRevision,
        byte[]? rawPayload,
        string? payload,
        IReadOnlyList<IntakeError> errors
    )
    {
        if (rawPayload is null == payload is null)
        {
            throw ReceivedMessageInvariantErrors.ExactlyOnePayloadForm();
        }

        MachineRegistrationId = machineRegistrationId;
        Topic = topic;
        SampledAt = sampledAt;
        ReceivedAt = receivedAt;
        SchemaRevision = schemaRevision;
        RawPayload = rawPayload;
        Payload = payload;
        Errors = errors;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid? MachineRegistrationId { get; private set; }
    public string Topic { get; private set; }
    public DateTime? SampledAt { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public int? SchemaRevision { get; private set; }

    /// <summary>The bytes of a payload that <c>jsonb</c> cannot store. It is <see langword="null"/> when
    /// <see cref="Payload"/> is set.</summary>
    public byte[]? RawPayload { get; private set; }

    /// <summary>The payload as JSON text. It is <see langword="null"/> when the bytes are not valid JSON, when the text
    /// is unstorable as <c>jsonb</c>, or after a processing failure. Then <see cref="RawPayload"/> holds the
    /// bytes.</summary>
    public string? Payload { get; private set; }

    public IReadOnlyList<IntakeError> Errors { get; private set; }
}
