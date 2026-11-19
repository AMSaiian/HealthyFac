using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.Machines;

/// <summary>
/// Tells the services that follow the machine go-live date (Telemetry) that the date changed. A consumer drops a
/// message whose <see cref="ChangedAt"/> is not after the last one it stored.
/// </summary>
public sealed record MachineGoLiveDateChanged : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:MachineGoLiveDateChanged";

    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }

    public required DateOnly ExpectedGoLiveOn { get; init; }

    /// <summary>
    /// The UTC instant of the local midnight of <see cref="ExpectedGoLiveOn"/> in the company time zone. A midnight
    /// in a skipped hour moves forward by the gap length.
    /// </summary>
    public required DateTime GoLiveStartsAt { get; init; }

    public required DateTime ChangedAt { get; init; }
}

public class MachineGoLiveDateChangedValidator : AbstractValidator<MachineGoLiveDateChanged>
{
    public MachineGoLiveDateChangedValidator()
    {
        RuleFor(message => message.MachineId)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.MachineIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.ChangedAt)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.ChangedAtRequired);
    }
}
