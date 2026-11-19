namespace HealthyFac.Telemetry.Domain.Evaluation.Statistics;

/// <summary>
/// The values of the detection options that the statistics math needs. The domain has no options type, so the
/// application layer copies the values into this record.
/// </summary>
public sealed record StatisticsSettings
{
    public required int WarmUpSamples { get; init; }

    public required double SlowTauSeconds { get; init; }

    public required double FastTauSeconds { get; init; }

    public required double LevelTauSeconds { get; init; }

    public required double TrendTauSeconds { get; init; }

    public required double GapSeconds { get; init; }

    /// <summary>The share of the metric range that the sigma never falls below.</summary>
    public required double SigmaFloorShare { get; init; }
}
