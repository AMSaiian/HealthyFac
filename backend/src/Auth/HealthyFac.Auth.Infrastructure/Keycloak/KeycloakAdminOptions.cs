using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Auth.Infrastructure.Keycloak;

public sealed class KeycloakAdminOptions
{
    public const string SectionName = "KeycloakAdmin";

    public required string BaseAddress { get; init; }

    public required string Realm { get; init; }

    public required string SpaClientId { get; init; }

    public required string SpaBaseUrl { get; init; }

    public int SetPasswordLifespanSeconds { get; init; } = KeycloakAdminConstants.DefaultSetPasswordLifespanSeconds;
}

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
