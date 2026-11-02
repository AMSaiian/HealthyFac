using FluentValidation;

namespace HealthyFac.Core.Application.Employees.CompleteProvisioning;

public class IdentityProvisionedValidator : AbstractValidator<IdentityProvisioned>
{
    public IdentityProvisionedValidator()
    {
        RuleFor(message => message.EmployeeId)
            .NotEmpty()
            .WithMessage(EmployeeProvisioningErrors.Validation.EmployeeIdRequired);

        RuleFor(message => message.IdentityUserId)
            .NotEmpty()
            .WithMessage(EmployeeProvisioningErrors.Validation.IdentityUserIdRequired);
    }
}
