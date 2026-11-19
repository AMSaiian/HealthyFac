namespace HealthyFac.Telemetry.Domain.Evaluation.Phases;

/// <summary>
/// The values of the detection options that the phase tracking needs. The domain has no options type, so the
/// application layer copies the values into this record.
/// </summary>
public sealed record PhaseSettings
{
    /// <summary>A mismatch shorter than this time keeps the current phase.</summary>
    public required double PhaseDwellSeconds { get; init; }

    /// <summary>The time after a switch in which no statistics learn.</summary>
    public required double SettleTimeSeconds { get; init; }
}
