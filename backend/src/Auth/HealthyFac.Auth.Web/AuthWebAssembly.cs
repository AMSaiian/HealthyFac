using System.Reflection;

namespace HealthyFac.Auth.Web;

/// <summary>
/// Labels the Auth Web assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class AuthWebAssembly
{
    public static readonly Assembly Instance = typeof(AuthWebAssembly).Assembly;
}
