using System.Reflection;

namespace HealthyFac.Telemetry.Web;

/// <summary>
/// Labels the Telemetry Web assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class TelemetryWebAssembly
{
    public static readonly Assembly Instance = typeof(TelemetryWebAssembly).Assembly;
}
