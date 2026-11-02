using FluentValidation;
using HealthyFac.Core.Application.Companies;
using HealthyFac.Core.Application.Employees;

namespace HealthyFac.Core.Infrastructure.Database.Seeding;

public sealed class TestTenantOptionsValidator : AbstractValidator<TestTenantOptions>
{
    public TestTenantOptionsValidator()
    {
        RuleFor(options => options.CompanyId).NotEmpty();
        RuleFor(options => options.SupervisorIdentityId).NotEmpty();
        RuleFor(options => options.SupervisorEmployeeId).NotEmpty();

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
