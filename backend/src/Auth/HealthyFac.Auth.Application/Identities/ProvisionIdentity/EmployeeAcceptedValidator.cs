using FluentValidation;

namespace HealthyFac.Auth.Application.Identities.ProvisionIdentity;

public class EmployeeAcceptedValidator : AbstractValidator<EmployeeAccepted>
{
    public EmployeeAcceptedValidator()
    {
        RuleFor(message => message.EmployeeId)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.EmployeeIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.EmailRequired)
            .EmailAddress()
            .WithMessage(IdentityErrors.Validation.EmailInvalid)
            .MaximumLength(IdentityValidationConstants.EmailMaxLength);

        RuleFor(message => message.FirstName)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.FirstNameRequired)
            .MaximumLength(IdentityValidationConstants.NameMaxLength);

        RuleFor(message => message.LastName)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.LastNameRequired)
            .MaximumLength(IdentityValidationConstants.NameMaxLength);

        RuleFor(message => message.RoleCode)
            .NotEmpty()
            .WithMessage(IdentityErrors.Validation.RoleCodeRequired);
    }
}
