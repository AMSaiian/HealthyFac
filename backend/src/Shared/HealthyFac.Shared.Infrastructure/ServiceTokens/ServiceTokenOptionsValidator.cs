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
            .GreaterThanOrEqualTo(0)
            .WithMessage(ServiceTokenValidationMessages.RefreshMarginInvalid);
    }
}
