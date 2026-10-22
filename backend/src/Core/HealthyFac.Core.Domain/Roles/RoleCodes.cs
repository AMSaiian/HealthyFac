namespace HealthyFac.Core.Domain.Roles;

/// <summary>
/// Defines the stable codes of the global roles. The codes are also the Keycloak client role names, so never
/// rename one in place.
/// </summary>
public static class RoleCodes
{
    public const string Worker = "worker";
    public const string Supervisor = "supervisor";
}
