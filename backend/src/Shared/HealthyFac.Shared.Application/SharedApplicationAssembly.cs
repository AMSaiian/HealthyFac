using System.Reflection;

namespace HealthyFac.Shared.Application;

/// <summary>
/// Labels the Shared Application assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class SharedApplicationAssembly
{
    public static readonly Assembly Instance = typeof(SharedApplicationAssembly).Assembly;
}
