using System.Reflection;

namespace HealthyFac.Telemetry.Contracts;

/// <summary>
/// Labels the Telemetry Contracts assembly for assembly scans (architecture tests, convention checks).
/// </summary>
public static class TelemetryContractsAssembly
{
    public static readonly Assembly Instance = typeof(TelemetryContractsAssembly).Assembly;
}
