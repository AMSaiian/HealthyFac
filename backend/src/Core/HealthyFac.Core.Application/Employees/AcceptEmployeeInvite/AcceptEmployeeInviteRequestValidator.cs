using FluentValidation;

namespace HealthyFac.Core.Application.Employees.AcceptEmployeeInvite;

public class AcceptEmployeeInviteRequestValidator : AbstractValidator<AcceptEmployeeInviteRequest>
{
    public AcceptEmployeeInviteRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.TokenRequired);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.FirstNameRequired)
            .MaximumLength(EmployeeValidationConstants.NameMaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.LastNameRequired)
            .MaximumLength(EmployeeValidationConstants.NameMaxLength);
    }
}
