using HealthyFac.Core.Domain.Roles;
using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.People;

public sealed class PersonSeed
{
    public required string Key { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string Email { get; init; }

    public required RoleCode Role { get; init; }

    public required RelativeMoment InvitedAt { get; init; }

    /// <summary>The key of the inviter. It is <see langword="null"/> for the first supervisor only.</summary>
    public required string? InvitedBy { get; init; }

    public required RelativeMoment AcceptedAt { get; init; }
}
