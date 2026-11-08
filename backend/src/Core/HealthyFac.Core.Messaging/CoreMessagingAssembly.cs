using System.Reflection;

namespace HealthyFac.Core.Messaging;

/// <summary>
/// Labels the Core Messaging assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class CoreMessagingAssembly
{
    public static readonly Assembly Instance = typeof(CoreMessagingAssembly).Assembly;
}
