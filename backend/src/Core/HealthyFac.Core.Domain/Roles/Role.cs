using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Roles;

/// <summary>
/// Represents a global employee role. The system seeds the rows, so the entity has no audit fields. Code
/// refers to a role through its <see cref="Code"/>, because every environment seeds the role with its own
/// generated <see cref="Id"/>. The code is also the name of the matching Keycloak client role.
/// </summary>
public class Role : IEntity
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public required RoleCode Code { get; init; }
    public required string Name { get; init; }
}
