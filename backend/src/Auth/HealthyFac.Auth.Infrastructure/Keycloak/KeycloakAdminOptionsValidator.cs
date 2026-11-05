using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Auth.Infrastructure.Keycloak;

public sealed class KeycloakAdminOptionsValidator : AbstractValidator<KeycloakAdminOptions>
{
    public KeycloakAdminOptionsValidator()
    {
        RuleFor(options => options.BaseAddress)
            .MustBeAbsoluteHttpUri()
            .WithMessage(KeycloakValidationMessages.BaseAddressInvalid);

        RuleFor(options => options.Realm)
            .NotEmpty()
            .WithMessage(KeycloakValidationMessages.RealmRequired);

        RuleFor(options => options.SpaClientId)
            .NotEmpty()
            .WithMessage(KeycloakValidationMessages.SpaClientIdRequired);

        RuleFor(options => options.SpaBaseUrl)
            .MustBeAbsoluteHttpUri()
            .WithMessage(KeycloakValidationMessages.SpaBaseUrlInvalid);

        RuleFor(options => options.SetPasswordLifespanSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(KeycloakValidationMessages.SetPasswordLifespanInvalid);
    }
}
