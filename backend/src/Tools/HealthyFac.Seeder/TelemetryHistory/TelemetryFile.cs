namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>
/// The content of <c>telemetry.json</c>: the window of the telemetry history, its random seed, the marker message, and
/// the events that shape the readings of the machines.
/// </summary>
public sealed class TelemetryFile
{
    /// <summary>The length of the history before the start of the job, in hours.</summary>
    public required int WindowHours { get; init; }

    public required int SampleIntervalSeconds { get; init; }

    /// <summary>The seed of the random source. Each machine adds its index in <c>machines.json</c>.</summary>
    public required int RandomSeed { get; init; }

    /// <summary>The ID of the message that marks the telemetry history as complete.</summary>
    public required Guid MarkerMessageId { get; init; }

    public required IReadOnlyList<DrainSeed> Drains { get; init; }

    public required IReadOnlyList<StepSeed> Steps { get; init; }

    public required IReadOnlyList<GapSeed> Gaps { get; init; }

    public required IReadOnlyList<OverrideSeed> Overrides { get; init; }
}
