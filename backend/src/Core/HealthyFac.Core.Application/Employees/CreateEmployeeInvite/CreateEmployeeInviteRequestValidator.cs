using FluentValidation;

namespace HealthyFac.Core.Application.Employees.CreateEmployeeInvite;

public class CreateEmployeeInviteRequestValidator : AbstractValidator<CreateEmployeeInviteRequest>
{
    public CreateEmployeeInviteRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.EmailRequired)
            .EmailAddress()
            .WithMessage(EmployeeInviteErrors.Validation.EmailInvalid)
            .MaximumLength(EmployeeValidationConstants.EmailMaxLength);

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(x => x.RoleCode)
            .NotNull()
            .WithMessage(EmployeeInviteErrors.Validation.RoleCodeRequired);

        RuleFor(x => x.DepartmentIds)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.DepartmentIdsRequired);

        RuleForEach(x => x.DepartmentIds)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.DepartmentIdRequired);
    }
}
