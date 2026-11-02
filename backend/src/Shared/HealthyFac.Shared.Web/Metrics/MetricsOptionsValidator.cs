using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Shared.Web.Metrics;

public sealed class MetricsOptionsValidator : AbstractValidator<MetricsOptions>
{
    public MetricsOptionsValidator()
    {
        RuleFor(options => options.Endpoint)
            .MustBeAbsoluteHttpUri()
            .When(options => !string.IsNullOrWhiteSpace(options.Endpoint));

        RuleForEach(options => options.AdditionalMeters)
            .NotEmpty()
            .WithMessage(MetricsValidationMessages.MeterNameRequired);
    }
}
