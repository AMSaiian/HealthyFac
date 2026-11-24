using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>An offset that is added to the value of a numeric metric while the step lasts.</summary>
public sealed class StepSeed
{
    public required string Key { get; init; }

    public required string Machine { get; init; }

    public required string Metric { get; init; }

    public required double Offset { get; init; }

    public required RelativeMoment From { get; init; }

    public required RelativeMoment Until { get; init; }
}
