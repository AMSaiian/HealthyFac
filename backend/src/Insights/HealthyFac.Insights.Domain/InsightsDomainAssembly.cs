using System.Reflection;

namespace HealthyFac.Insights.Domain;

/// <summary>
/// Labels the Insights Domain assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class InsightsDomainAssembly
{
    public static readonly Assembly Instance = typeof(InsightsDomainAssembly).Assembly;
}
