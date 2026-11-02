using System.Reflection;

namespace HealthyFac.Core.Infrastructure;

/// <summary>
/// Labels the Core Infrastructure assembly for assembly scans (entity configurations). A scan reads the
/// assembly through this class, so moving or renaming a business type never changes what the scan finds.
/// </summary>
public static class CoreInfrastructureAssembly
{
    public static readonly Assembly Instance = typeof(CoreInfrastructureAssembly).Assembly;
}
