using System.Net;
using FluentValidation;

namespace HealthyFac.Shared.Web.Https;

public sealed class HttpsOptionsValidator : AbstractValidator<HttpsOptions>
{
    public HttpsOptionsValidator()
    {
        RuleFor(options => options.RedirectPort)
            .NotNull()
            .When(options => options.RedirectEnabled)
            .WithMessage(HttpsValidationMessages.RedirectPortRequired);

        RuleFor(options => options.RedirectPort)
            .InclusiveBetween(IPEndPoint.MinPort, IPEndPoint.MaxPort)
            .WithMessage(HttpsValidationMessages.PortOutOfRange);

        RuleFor(options => options.HstsMaxAgeDays)
            .GreaterThanOrEqualTo(1)
            .When(options => options.HstsEnabled)
            .WithMessage(HttpsValidationMessages.MaxAgeMustBePositive);

        RuleFor(options => options)
            .Must(MeetPreloadRequirements)
            .When(options => options.HstsPreload)
            .WithMessage(HttpsValidationMessages.PreloadRequirementsNotMet);
    }

    private static bool MeetPreloadRequirements(HttpsOptions options)
        => options is
        {
            HstsEnabled: true,
            HstsIncludeSubDomains: true,
            HstsMaxAgeDays: >= HttpsConstants.PreloadMinimumMaxAgeDays
        };
}
