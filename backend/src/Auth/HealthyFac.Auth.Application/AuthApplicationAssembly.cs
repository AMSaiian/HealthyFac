using System.Reflection;

namespace HealthyFac.Auth.Application;

/// <summary>
/// Labels the Auth Application assembly for assembly scans (use cases, decorators, validators). A scan reads the
/// assembly through this class, so moving or renaming a business type never changes what the scan finds.
/// </summary>
public static class AuthApplicationAssembly
{
    public static readonly Assembly Instance = typeof(AuthApplicationAssembly).Assembly;
}
