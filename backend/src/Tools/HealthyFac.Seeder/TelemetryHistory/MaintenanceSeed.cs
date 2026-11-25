using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.TelemetryHistory;

public sealed class MaintenanceSeed
{
    /// <summary>The key of the person who changes the state of the machine.</summary>
    public required string By { get; init; }

    public required RelativeMoment From { get; init; }

    public required RelativeMoment Until { get; init; }
}
