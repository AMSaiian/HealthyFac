using FluentValidation;

namespace HealthyFac.Auth.Application.Identities.Messages;

public class EmployeeDeactivatedValidator : AbstractValidator<EmployeeDeactivated>
{
    public EmployeeDeactivatedValidator()
    {
        RuleFor(message => message.EmployeeId)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.EmployeeIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.IdentityUserId)
            .NotEqual(Guid.Empty)
            .When(message => message.IdentityUserId is not null)
            .WithMessage(IdentityErrors.Validation.IdentityUserIdInvalid);

        RuleFor(message => message.DeactivatedAt)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.DeactivatedAtRequired);
    }
}
