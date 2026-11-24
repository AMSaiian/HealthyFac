using HealthyFac.Telemetry.Domain.Intake;

namespace HealthyFac.Seeder.TelemetryHistory.Persistence;

/// <summary>The rows of one history message: the received message and its readings.</summary>
public sealed class HistoryRows
{
    public required ReceivedMessage Received { get; init; }

    /// <summary>The ID to store instead of the ID of <see cref="Received"/>, or <see langword="null"/>.</summary>
    public Guid? MessageId { get; init; }

    public required IReadOnlyList<NumericReading> Numeric { get; init; }

    public required IReadOnlyList<DiscreteReading> Discrete { get; init; }

    public required IReadOnlyList<StatusReading> Statuses { get; init; }
}
