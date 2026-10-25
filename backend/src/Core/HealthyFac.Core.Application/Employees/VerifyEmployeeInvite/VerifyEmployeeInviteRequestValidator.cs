using FluentValidation;

namespace HealthyFac.Core.Application.Employees.VerifyEmployeeInvite;

public class VerifyEmployeeInviteRequestValidator : AbstractValidator<VerifyEmployeeInviteRequest>
{
    public VerifyEmployeeInviteRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.TokenRequired);
    }
}
