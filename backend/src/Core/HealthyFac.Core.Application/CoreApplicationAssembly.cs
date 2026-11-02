using System.Reflection;

namespace HealthyFac.Core.Application;

/// <summary>
/// Labels the Core Application assembly for assembly scans (use cases, decorators, validators). A scan reads the
/// assembly through this class, so moving or renaming a business type never changes what the scan finds.
/// </summary>
public static class CoreApplicationAssembly
{
    public static readonly Assembly Instance = typeof(CoreApplicationAssembly).Assembly;
}
