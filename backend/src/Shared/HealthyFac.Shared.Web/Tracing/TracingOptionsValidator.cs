using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Shared.Web.Tracing;

public sealed class TracingOptionsValidator : AbstractValidator<TracingOptions>
{
    public TracingOptionsValidator()
    {
        RuleFor(options => options.SamplingRatio)
            .InclusiveBetween(0.0, 1.0)
            .WithMessage(TracingValidationMessages.SamplingRatioOutOfRange);

        RuleFor(options => options.Endpoint)
            .MustBeAbsoluteHttpUri()
            .When(options => !string.IsNullOrWhiteSpace(options.Endpoint));

        RuleForEach(options => options.AdditionalSources)
            .NotEmpty()
            .WithMessage(TracingValidationMessages.SourceNameRequired);
    }
}
