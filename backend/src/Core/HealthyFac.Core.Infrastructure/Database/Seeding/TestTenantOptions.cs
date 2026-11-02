namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>
/// Describes the test company and its first supervisor. The same values feed Keycloak (the realm import) and Core
/// (this seed), so the supervisor can sign in as soon as both services run. The IDs are fixed on purpose: the
/// company ID is also the alias of the Keycloak organization, and the supervisor IDs are the <c>sub</c> and
/// <c>employee_id</c> claims. This is the approved seed exception of the ID generation rule.
/// </summary>
public sealed class TestTenantOptions
{
    public required Guid CompanyId { get; init; }

    public required string CompanyName { get; init; }

    public required Guid SupervisorIdentityId { get; init; }

    public required Guid SupervisorEmployeeId { get; init; }

    public required string SupervisorEmail { get; init; }

    public required string SupervisorFirstName { get; init; }

    public required string SupervisorLastName { get; init; }
}
