using FluentValidation;
using HealthyFac.Shared.Application.Configuration;

namespace HealthyFac.Insights.Infrastructure.Telemetry;

public sealed class TelemetryClientOptions
{
    public const string SectionName = "TelemetryClient";

    /// <summary>The address of the HTTP port of Telemetry.</summary>
    public required string BaseAddress { get; init; }
}

public sealed class TelemetryClientOptionsValidator : AbstractValidator<TelemetryClientOptions>
{
    public TelemetryClientOptionsValidator()
    {
        RuleFor(options => options.BaseAddress)
            .MustBeAbsoluteHttpUri()
            .WithMessage(TelemetryClientMessages.BaseAddressInvalid);
    }
}
