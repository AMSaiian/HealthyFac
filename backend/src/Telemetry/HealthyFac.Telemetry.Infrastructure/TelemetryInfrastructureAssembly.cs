using System.Reflection;

namespace HealthyFac.Telemetry.Infrastructure;

/// <summary>
/// Labels the Telemetry Infrastructure assembly for assembly scans (entity configurations). A scan reads the
/// assembly through this class, so moving or renaming a business type never changes what the scan finds.
/// </summary>
public static class TelemetryInfrastructureAssembly
{
    public static readonly Assembly Instance = typeof(TelemetryInfrastructureAssembly).Assembly;
}
