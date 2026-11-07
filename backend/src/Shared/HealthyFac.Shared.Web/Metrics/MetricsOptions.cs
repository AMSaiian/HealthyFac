using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Shared.Web.Metrics;

/// <summary>
/// Configures the OpenTelemetry meter provider and its OTLP exporter. Set <see cref="Endpoint"/> to export the
/// metrics; it is the same collector endpoint as the traces.
/// </summary>
public sealed class MetricsOptions
{
    public const string SectionName = "Metrics";

    /// <summary>Set to false only to switch metrics off entirely, for example in a test host.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>The OTLP collector endpoint. An empty value keeps the measurements in process.</summary>
    public string? Endpoint { get; init; }

    /// <summary>Extra meter names to listen to, beyond the shared meters (e.g., <c>Npgsql</c>).</summary>
    public string[] AdditionalMeters { get; init; } = [];
}

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
