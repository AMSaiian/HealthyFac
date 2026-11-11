using System.Reflection;

namespace HealthyFac.Insights.Messaging;

/// <summary>
/// Labels the Insights Messaging assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class InsightsMessagingAssembly
{
    public static readonly Assembly Instance = typeof(InsightsMessagingAssembly).Assembly;
}
