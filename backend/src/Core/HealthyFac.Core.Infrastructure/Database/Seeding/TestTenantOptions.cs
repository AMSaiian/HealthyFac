using FluentValidation;
using HealthyFac.Core.Application.Companies;
using HealthyFac.Core.Application.Employees;

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

    public required IReadOnlyList<Guid> DepartmentIds { get; init; }

    public required Guid SupervisorIdentityId { get; init; }

    public required Guid SupervisorEmployeeId { get; init; }

    public required string SupervisorEmail { get; init; }

    public required string SupervisorFirstName { get; init; }

    public required string SupervisorLastName { get; init; }
}

public sealed class TestTenantOptionsValidator : AbstractValidator<TestTenantOptions>
{
    public TestTenantOptionsValidator()
    {
        RuleFor(options => options.CompanyId).NotEmpty();
        RuleFor(options => options.SupervisorIdentityId).NotEmpty();
        RuleFor(options => options.SupervisorEmployeeId).NotEmpty();

        RuleFor(options => options.DepartmentIds)
            .Cascade(CascadeMode.Stop)
            .Must(departmentIds => departmentIds.Count == SeedingValidationConstants.Counts.TestTenantDepartments)
            .WithMessage(SeedingValidationConstants.Messages.TestTenantDepartmentCount)
            .Must(departmentIds => departmentIds.All(departmentId => departmentId != Guid.Empty))
            .WithMessage(SeedingValidationConstants.Messages.TestTenantDepartmentIdEmpty)
            .Must(departmentIds => departmentIds.Distinct().Count() == departmentIds.Count)
            .WithMessage(SeedingValidationConstants.Messages.TestTenantDepartmentIdDuplicated);

        RuleFor(options => options.CompanyName)
            .NotEmpty()
            .MaximumLength(CompanyValidationConstants.NameMaxLength);

        RuleFor(options => options.SupervisorEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(EmployeeValidationConstants.EmailMaxLength);

        RuleFor(options => options.SupervisorFirstName)
            .NotEmpty()
            .MaximumLength(EmployeeValidationConstants.NameMaxLength);

        RuleFor(options => options.SupervisorLastName)
            .NotEmpty()
            .MaximumLength(EmployeeValidationConstants.NameMaxLength);
    }
}
