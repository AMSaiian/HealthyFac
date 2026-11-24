using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>A time span in which the machine sends no message.</summary>
public sealed class GapSeed
{
    public required string Key { get; init; }

    public required string Machine { get; init; }

    public required RelativeMoment From { get; init; }

    public required RelativeMoment Until { get; init; }
}
