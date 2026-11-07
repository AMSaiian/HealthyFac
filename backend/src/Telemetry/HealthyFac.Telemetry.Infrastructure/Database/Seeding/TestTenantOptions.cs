using FluentValidation;

namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>
/// Describes the company and the departments of the test tenant. Core seeds the same IDs, so a seed machine of the
/// seed data file has the same department in both services. The IDs are fixed on purpose: Keycloak imports them too.
/// This is the approved seed exception of the ID generation rule.
/// </summary>
public sealed class TestTenantOptions
{
    public required Guid CompanyId { get; init; }

    public required IReadOnlyList<Guid> DepartmentIds { get; init; }
}

public sealed class TestTenantOptionsValidator : AbstractValidator<TestTenantOptions>
{
    public TestTenantOptionsValidator()
    {
        RuleFor(options => options.CompanyId).NotEmpty();

        RuleFor(options => options.DepartmentIds)
            .Cascade(CascadeMode.Stop)
            .Must(departmentIds => departmentIds.Count == SeedingValidationConstants.TestTenantDepartments)
            .WithMessage(SeedingValidationConstants.Messages.TestTenantDepartmentCount)
            .Must(departmentIds => departmentIds.All(departmentId => departmentId != Guid.Empty))
            .WithMessage(SeedingValidationConstants.Messages.TestTenantDepartmentIdEmpty)
            .Must(departmentIds => departmentIds.Distinct().Count() == departmentIds.Count)
            .WithMessage(SeedingValidationConstants.Messages.TestTenantDepartmentIdDuplicated);
    }
}
