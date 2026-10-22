using HealthyFac.Core.Domain.Roles;

namespace HealthyFac.Core.Infrastructure.Roles.Persistence;

/// <summary>
/// Lists the global roles that every environment needs. The initializer adds each role whose code is missing,
/// so the seed is safe to run on every start.
/// </summary>
internal static class RoleSeed
{
    public static IReadOnlyList<Role> Roles =>
    [
        new Role { Code = RoleCodes.Worker, Name = "Maintenance worker" },
        new Role { Code = RoleCodes.Supervisor, Name = "Supervisor" },
    ];
}
