using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Domain.Roles;

/// <summary>
/// Defines the stable codes of the global roles. The codes are also the Keycloak client role names, so never
/// rename one in place.
/// </summary>
public sealed class RoleCode : Enumeration<RoleCode>
{
    public static readonly RoleCode Worker = new("worker");
    public static readonly RoleCode Supervisor = new("supervisor");

    private RoleCode(string value)
        : base(value)
    {
    }
}
