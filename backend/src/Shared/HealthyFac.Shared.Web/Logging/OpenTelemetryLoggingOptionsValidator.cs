using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Shared.Web.Logging;

public sealed class OpenTelemetryLoggingOptionsValidator : AbstractValidator<OpenTelemetryLoggingOptions>
{
    public OpenTelemetryLoggingOptionsValidator()
    {
        RuleFor(options => options.Endpoint)
            .MustBeAbsoluteHttpUri()
            .When(options => !string.IsNullOrWhiteSpace(options.Endpoint));
    }
}
