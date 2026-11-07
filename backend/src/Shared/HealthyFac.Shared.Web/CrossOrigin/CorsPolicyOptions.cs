using FluentValidation;
using HealthyFac.Shared.Application.Configuration;

namespace HealthyFac.Shared.Web.CrossOrigin;

/// <summary>
/// Configures browser cross-origin access for a single service.
/// Leave <see cref="IsEnabled"/> false wherever a gateway owns the policy. Two layers emitting the headers
/// produce a duplicate <c>Access-Control-Allow-Origin</c>, which every browser rejects.
/// </summary>
public sealed class CorsPolicyOptions
{
    public const string SectionName = "Cors";

    /// <summary>Set to true only where this service answers browsers directly.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>The browser origins allowed to call this service, for example "http://localhost:4200".</summary>
    public string[] AllowedOrigins { get; init; } = [];

    /// <summary>
    /// Allows the browser to send cookies and TLS client certificates. Leave this false for bearer token
    /// authentication: the <c>Authorization</c> header is an ordinary header to CORS, covered by
    /// <c>AllowAnyHeader</c>, and is not a credential. Enable it only for a cookie session, such as a
    /// backend for frontend. Enabling it also forbids the wildcard origin.
    /// </summary>
    public bool IsCredentialsAllowed { get; init; }

    /// <summary>
    /// How long, in seconds, the browser keeps the answer to a preflight. The browser then sends no preflight before
    /// each call. While the policy is enabled, the value must be greater than 0 and at most
    /// <see cref="CorsConstants.MaxPreflightMaxAgeSeconds"/>.
    /// </summary>
    public int PreflightMaxAgeSeconds { get; init; }
}

public sealed class CorsPolicyOptionsValidator : AbstractValidator<CorsPolicyOptions>
{
    public CorsPolicyOptionsValidator()
    {
        When(
            options => options.IsEnabled,
            () =>
            {
                RuleFor(options => options.AllowedOrigins)
                    .NotEmpty()
                    .WithMessage(CorsValidationMessages.OriginsRequired);

                RuleForEach(options => options.AllowedOrigins)
                    .Must(BeWildcardOrOrigin)
                    .WithMessage(CorsValidationMessages.OriginInvalid);

                RuleFor(options => options.PreflightMaxAgeSeconds)
                    .GreaterThan(valueToCompare: 0)
                    .WithMessage(CorsValidationMessages.PreflightMaxAgeRequired)
                    .LessThanOrEqualTo(CorsConstants.MaxPreflightMaxAgeSeconds)
                    .WithMessage(CorsValidationMessages.PreflightMaxAgeAboveBrowserLimit);

                RuleFor(options => options.AllowedOrigins)
                    .Must(origins => !origins.Contains(CorsConstants.Wildcard))
                    .When(options => options.IsCredentialsAllowed)
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
