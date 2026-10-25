using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Roles;

/// <summary>
/// Represents a global employee role. The system seeds the rows, so the entity has no audit fields. Code
/// refers to a role through its <see cref="Code"/>, because the database generates a different
/// <see cref="Id"/> in every environment. The code is also the name of the matching Keycloak client role.
/// </summary>
public class Role : IEntity
{
    public Guid Id { get; private set; } = Guid.Empty;
    public required RoleCode Code { get; init; }
    public required string Name { get; init; }
}
