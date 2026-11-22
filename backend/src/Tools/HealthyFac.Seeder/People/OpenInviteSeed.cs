using HealthyFac.Core.Domain.Roles;
using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.People;

/// <summary>An invite that nobody has accepted yet.</summary>
public sealed class OpenInviteSeed
{
    public required string Email { get; init; }

    public required RoleCode Role { get; init; }

    public required string InvitedBy { get; init; }

    public required RelativeMoment InvitedAt { get; init; }
}
