using FluentValidation;

namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>
/// Describes the company of the test tenant. Core seeds the same ID, so a seed machine of the seed data file has the
/// same company in both services. The ID is fixed on purpose: Keycloak imports it too. This is the approved seed
/// exception of the ID generation rule.
/// </summary>
public sealed class TestTenantOptions
{
    public required Guid CompanyId { get; init; }
}

public sealed class TestTenantOptionsValidator : AbstractValidator<TestTenantOptions>
{
    public TestTenantOptionsValidator()
    {
        RuleFor(options => options.CompanyId).NotEmpty();
    }
}
