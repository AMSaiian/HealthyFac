using System.Net;
using FluentValidation;

namespace HealthyFac.Shared.Web.Https;

/// <summary>
/// Configures transport security for the service.
/// Behind a gateway that terminates TLS, keep <see cref="IsRedirectEnabled"/> false and let the gateway
/// redirect. The service still sends HSTS, so browsers refuse plain HTTP on later visits.
/// </summary>
public sealed class HttpsOptions
{
    public const string SectionName = "Https";

    /// <summary>Redirects plain HTTP requests to HTTPS. Requires <see cref="RedirectPort"/>.</summary>
    public bool IsRedirectEnabled { get; init; }

    /// <summary>
    /// The HTTPS port to redirect to. Without it the middleware cannot build the target URL and
    /// silently passes the request through.
    /// </summary>
    public int? RedirectPort { get; init; }

    /// <summary>Sends the Strict-Transport-Security header. Browsers ignore it over plain HTTP.</summary>
    public bool IsHstsEnabled { get; init; } = true;

    /// <summary>How long a browser must keep using HTTPS for this host.</summary>
    public int HstsMaxAgeDays { get; init; } = 365;

    /// <summary>Applies the policy to every subdomain.</summary>
    public bool IsHstsSubDomainsIncluded { get; init; } = true;

    /// <summary>
    /// Requests inclusion in the browser preload lists. Set this only when the whole domain, including
    /// every subdomain, serves HTTPS. Removal from a preload list takes months.
    /// </summary>
    public bool IsHstsPreloaded { get; init; }
}

public sealed class HttpsOptionsValidator : AbstractValidator<HttpsOptions>
{
    public HttpsOptionsValidator()
    {
        RuleFor(options => options.RedirectPort)
            .NotNull()
            .When(options => options.IsRedirectEnabled)
            .WithMessage(HttpsValidationMessages.RedirectPortRequired);

        RuleFor(options => options.RedirectPort)
            .InclusiveBetween(IPEndPoint.MinPort, IPEndPoint.MaxPort)
            .WithMessage(HttpsValidationMessages.PortOutOfRange);

        RuleFor(options => options.HstsMaxAgeDays)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .When(options => options.IsHstsEnabled)
            .WithMessage(HttpsValidationMessages.MaxAgeMustBePositive);

        RuleFor(options => options)
            .Must(MeetPreloadRequirements)
            .When(options => options.IsHstsPreloaded)
            .WithMessage(HttpsValidationMessages.PreloadRequirementsNotMet);
    }

    private static bool MeetPreloadRequirements(HttpsOptions options)
        => options is
        {
            IsHstsEnabled: true,
            IsHstsSubDomainsIncluded: true,
            HstsMaxAgeDays: >= HttpsConstants.PreloadMinimumMaxAgeDays,
        };
}
