using System.Reflection;

namespace HealthyFac.Insights.Infrastructure;

/// <summary>
/// Labels the Insights Infrastructure assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class InsightsInfrastructureAssembly
{
    public static readonly Assembly Instance = typeof(InsightsInfrastructureAssembly).Assembly;
}
