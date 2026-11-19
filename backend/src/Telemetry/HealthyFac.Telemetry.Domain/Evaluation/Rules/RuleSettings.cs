namespace HealthyFac.Telemetry.Domain.Evaluation.Rules;

/// <summary>
/// The values of the detection options that the rules need. The domain has no options type, so the application layer
/// copies the values into this record.
/// </summary>
public sealed record RuleSettings
{
    /// <summary>The number of samples in a row that opens, escalates, or resolves a rule.</summary>
    public required int Persistence { get; init; }

    public required double DriftLimit { get; init; }

    public required double DriftResolveLimit { get; init; }

    public required double HorizonSeconds { get; init; }

    public required double NoiseGuardFactor { get; init; }

    /// <summary>The time that a drift streak must last: <c>Persistence * FastTauSeconds</c>.</summary>
    public required double DriftDurationSeconds { get; init; }

    /// <summary>The time that a trend streak must last: <c>Persistence * TrendTauSeconds</c>.</summary>
    public required double TrendDurationSeconds { get; init; }
}
