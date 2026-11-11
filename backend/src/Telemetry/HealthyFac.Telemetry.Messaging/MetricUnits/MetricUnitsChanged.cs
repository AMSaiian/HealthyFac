using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Telemetry.Messaging.MetricUnits;

/// <summary>
/// Tells the services that cache the unit catalog (Insights) that Telemetry added or changed a unit, so they drop the
/// cached copy.
/// </summary>
public sealed record MetricUnitsChanged : IIntegrationMessage, IRequest
{
    public static string MessageType => "telemetry.v1:MetricUnitsChanged";

    public required DateTime ChangedAt { get; init; }
}

public class MetricUnitsChangedValidator : AbstractValidator<MetricUnitsChanged>
{
    public MetricUnitsChangedValidator()
    {
        RuleFor(message => message.ChangedAt)
            .NotEmpty()
            .WithMessage(MetricUnitMessageErrors.Validation.ChangedAtRequired);
    }
}
