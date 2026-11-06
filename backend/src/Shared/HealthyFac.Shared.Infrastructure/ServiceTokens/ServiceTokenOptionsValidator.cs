using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

public sealed class ServiceTokenOptionsValidator : AbstractValidator<ServiceTokenOptions>
{
    public ServiceTokenOptionsValidator()
    {
        RuleFor(options => options.TokenEndpoint)
            .MustBeAbsoluteHttpUri()
            .WithMessage(ServiceTokenValidationMessages.TokenEndpointInvalid);

        RuleFor(options => options.ClientId)
            .NotEmpty()
            .WithMessage(ServiceTokenValidationMessages.ClientIdRequired);

        RuleFor(options => options.ClientSecret)
            .NotEmpty()
            .WithMessage(ServiceTokenValidationMessages.ClientSecretRequired);

        RuleFor(options => options.RefreshMarginSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 0)
            .WithMessage(ServiceTokenValidationMessages.RefreshMarginInvalid);

        RuleFor(options => options.RefreshJitterSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 0)
            .WithMessage(ServiceTokenValidationMessages.RefreshJitterInvalid);

        RuleFor(options => options.LockTimeoutSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(ServiceTokenValidationMessages.LockTimeoutInvalid);

        RuleFor(options => options.LockPollMilliseconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(ServiceTokenValidationMessages.LockPollInvalid);

        RuleFor(options => options)
            .Must(options => TimeSpan.FromMilliseconds(options.LockPollMilliseconds)
                < TimeSpan.FromSeconds(options.LockTimeoutSeconds))
            .When(options => options.LockTimeoutSeconds > 0 && options.LockPollMilliseconds > 0)
            .WithName(nameof(ServiceTokenOptions.LockPollMilliseconds))
            .WithMessage(ServiceTokenValidationMessages.LockPollNotBelowTimeout);
    }
}
