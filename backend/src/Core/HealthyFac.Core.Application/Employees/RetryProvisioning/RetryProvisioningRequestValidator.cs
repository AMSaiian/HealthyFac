using FluentValidation;

namespace HealthyFac.Core.Application.Employees.RetryProvisioning;

public class RetryProvisioningRequestValidator : AbstractValidator<RetryProvisioningRequest>
{
    public RetryProvisioningRequestValidator()
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId)
            .NotEmpty()
            .WithMessage(EmployeeProvisioningErrors.Validation.EmployeeIdRequired);
    }
}
