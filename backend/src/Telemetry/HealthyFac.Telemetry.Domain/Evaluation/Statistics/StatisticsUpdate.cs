namespace HealthyFac.Telemetry.Domain.Evaluation.Statistics;

/// <summary>
/// Tells what one <see cref="MetricStatistics.Apply"/> call did.
/// </summary>
public sealed record StatisticsUpdate
{
    /// <summary>The sample was not after the last sample, so nothing changed.</summary>
    public required bool IsIgnored { get; init; }

    /// <summary>The tracker restarted from this sample: after a gap, or after a counter reset.</summary>
    public required bool IsTrackerRestarted { get; init; }

    public static StatisticsUpdate Ignored { get; } = new() { IsIgnored = true, IsTrackerRestarted = false };
}
