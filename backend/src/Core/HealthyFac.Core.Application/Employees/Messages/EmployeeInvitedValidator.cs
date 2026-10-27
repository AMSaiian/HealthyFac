using FluentValidation;

namespace HealthyFac.Core.Application.Employees.Messages;

public class EmployeeInvitedValidator : AbstractValidator<EmployeeInvited>
{
    public EmployeeInvitedValidator()
    {
        RuleFor(message => message.InviteId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.InviteIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.EmailRequired)
            .EmailAddress()
            .WithMessage(EmployeeInviteErrors.Validation.EmailInvalid)
            .MaximumLength(EmployeeValidationConstants.EmailMaxLength);
    }
}
