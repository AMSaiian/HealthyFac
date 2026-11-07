using System.Reflection;

namespace HealthyFac.Auth.Messaging;

/// <summary>
/// Labels the Auth Messaging assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class AuthMessagingAssembly
{
    public static readonly Assembly Instance = typeof(AuthMessagingAssembly).Assembly;
}
