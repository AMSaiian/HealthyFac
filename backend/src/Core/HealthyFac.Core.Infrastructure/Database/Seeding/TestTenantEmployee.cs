namespace HealthyFac.Core.Infrastructure.Database.Seeding;

internal sealed record TestTenantEmployee
{
    public required Guid Id { get; init; }

    public required Guid IdentityId { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }
}
