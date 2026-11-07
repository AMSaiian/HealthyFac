using System.Reflection;

namespace HealthyFac.Core.Web;

/// <summary>
/// Labels the Core Web assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class CoreWebAssembly
{
    public static readonly Assembly Instance = typeof(CoreWebAssembly).Assembly;
}
