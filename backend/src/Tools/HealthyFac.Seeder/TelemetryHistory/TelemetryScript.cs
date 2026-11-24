using HealthyFac.Seeder.Machines;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>The files that the anchor rules of mode 2 read: the telemetry script and the machines.</summary>
public sealed class TelemetryScript
{
    public required TelemetryFile Telemetry { get; init; }

    public required MachinesFile Machines { get; init; }
}
