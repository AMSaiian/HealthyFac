using System.Reflection;

namespace HealthyFac.Telemetry.Application;

/// <summary>
/// Labels the Telemetry Application assembly for assembly scans (use cases, decorators, validators). A scan reads the
/// assembly through this class, so moving or renaming a business type never changes what the scan finds.
/// </summary>
public static class TelemetryApplicationAssembly
{
    public static readonly Assembly Instance = typeof(TelemetryApplicationAssembly).Assembly;
}
