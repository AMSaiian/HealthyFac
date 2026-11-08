using System.Reflection;

namespace HealthyFac.Auth.Infrastructure;

/// <summary>
/// Labels the Auth Infrastructure assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class AuthInfrastructureAssembly
{
    public static readonly Assembly Instance = typeof(AuthInfrastructureAssembly).Assembly;
}
