using System.Reflection;

namespace HealthyFac.Core.Domain;

/// <summary>
/// Labels the Core Domain assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class CoreDomainAssembly
{
    public static readonly Assembly Instance = typeof(CoreDomainAssembly).Assembly;
}
