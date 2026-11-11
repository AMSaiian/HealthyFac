using FluentValidation;
using HealthyFac.Core.Application.Employees;

namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>
/// Describes the worker of the test company. The fixed IDs equal the <c>sub</c> and <c>employee_id</c> claims of the
/// worker in <c>docker/keycloak/dev/healthyfac-test-tenant.json</c>. This is the approved seed exception of the ID
/// generation rule.
/// </summary>
public sealed class TestTenantWorkerOptions
{
    public required Guid IdentityId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }
}

public sealed class TestTenantWorkerOptionsValidator : AbstractValidator<TestTenantWorkerOptions>
{
    public TestTenantWorkerOptionsValidator()
    {
        RuleFor(options => options.IdentityId).NotEmpty();
        RuleFor(options => options.EmployeeId).NotEmpty();

        RuleFor(options => options.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(EmployeeValidationConstants.EmailMaxLength);

        RuleFor(options => options.FirstName)
            .NotEmpty()
            .MaximumLength(EmployeeValidationConstants.NameMaxLength);

        RuleFor(options => options.LastName)
            .NotEmpty()
            .MaximumLength(EmployeeValidationConstants.NameMaxLength);
    }
}
