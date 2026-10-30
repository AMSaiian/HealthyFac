using FluentValidation;

namespace HealthyFac.Core.Application.Employees.Messages;

public class IdentityProvisioningFailedValidator : AbstractValidator<IdentityProvisioningFailed>
{
    public IdentityProvisioningFailedValidator()
    {
        RuleFor(message => message.EmployeeId)
            .NotEmpty()
            .WithMessage(EmployeeProvisioningErrors.Validation.EmployeeIdRequired);

        RuleFor(message => message.Reason)
            .NotEmpty()
            .WithMessage(EmployeeProvisioningErrors.Validation.ReasonRequired);
    }
}
