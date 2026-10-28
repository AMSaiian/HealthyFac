using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Shared.Web.CrossOrigin;

public sealed class CorsPolicyOptionsValidator : AbstractValidator<CorsPolicyOptions>
{
    public CorsPolicyOptionsValidator()
    {
        When(
            options => options.Enabled,
            () =>
            {
                RuleFor(options => options.AllowedOrigins)
                    .NotEmpty()
                    .WithMessage(CorsValidationMessages.OriginsRequired);

                RuleForEach(options => options.AllowedOrigins)
                    .Must(BeWildcardOrOrigin)
                    .WithMessage(CorsValidationMessages.OriginInvalid);

                RuleFor(options => options.PreflightMaxAgeSeconds)
                    .GreaterThan(0)
                    .WithMessage(CorsValidationMessages.PreflightMaxAgeRequired)
                    .LessThanOrEqualTo(CorsConstants.MaxPreflightMaxAgeSeconds)
                    .WithMessage(CorsValidationMessages.PreflightMaxAgeAboveBrowserLimit);

                RuleFor(options => options.AllowedOrigins)
                    .Must(origins => !origins.Contains(CorsConstants.Wildcard))
                    .When(options => options.AllowCredentials)
                    .WithMessage(CorsValidationMessages.CredentialsWithWildcardOrigin);
            });
    }

    private static bool BeWildcardOrOrigin(string origin)
        => origin == CorsConstants.Wildcard || IsOrigin(origin);

    private static bool IsOrigin(string value)
        => UriRuleBuilderExtensions.TryCreateAbsoluteHttpUri(value, out var uri)
            && uri.PathAndQuery == CorsConstants.RootPath
            && !value.EndsWith(CorsConstants.RootPath);
}
