using System.Reflection;

namespace HealthyFac.Telemetry.Domain;

/// <summary>
/// Labels the Telemetry Domain assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class TelemetryDomainAssembly
{
    public static readonly Assembly Instance = typeof(TelemetryDomainAssembly).Assembly;
}
