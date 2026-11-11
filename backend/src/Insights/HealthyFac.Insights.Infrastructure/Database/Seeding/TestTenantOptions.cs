using FluentValidation;

namespace HealthyFac.Insights.Infrastructure.Database.Seeding;

/// <summary>
/// Describes the test tenant. Core and Document seed the same company, so the demo files of the snapshot have the
/// same company in every service. The IDs are fixed on purpose: Keycloak imports them too. This is the approved seed
/// exception of the ID generation rule.
/// </summary>
public sealed class TestTenantOptions
{
    public required Guid CompanyId { get; init; }

    /// <summary>The Keycloak user ID of the test supervisor. The seeded rows are created as this user.</summary>
    public required Guid SupervisorIdentityId { get; init; }
}

public sealed class TestTenantOptionsValidator : AbstractValidator<TestTenantOptions>
{
    public TestTenantOptionsValidator()
    {
        RuleFor(options => options.CompanyId).NotEmpty();
        RuleFor(options => options.SupervisorIdentityId).NotEmpty();
    }
}
