using FluentValidation;

namespace HealthyFac.Core.Application.Employees.ResendEmployeeInvite;

public class ResendEmployeeInviteRequestValidator : AbstractValidator<ResendEmployeeInviteRequest>
{
    public ResendEmployeeInviteRequestValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(x => x.InviteId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.InviteIdRequired);
    }
}
