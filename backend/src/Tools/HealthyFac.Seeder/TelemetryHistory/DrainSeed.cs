using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>A module of a machine that drains from <see cref="From"/> and is repaired in a maintenance.</summary>
public sealed class DrainSeed
{
    public required string Key { get; init; }

    public required string Machine { get; init; }

    public required string Module { get; init; }

    public required RelativeMoment From { get; init; }

    /// <summary>The run time that a full drain needs, in hours.</summary>
    public required double DrainHours { get; init; }

    public required MaintenanceSeed Maintenance { get; init; }

    /// <summary>The work order that handles the drain. A drain without it has no work order and no link.</summary>
    public DrainWorkOrderSeed? WorkOrder { get; init; }
}
