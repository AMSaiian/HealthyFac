using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.Machines;

/// <summary>
/// Tells the services that follow the machine state (Telemetry) the new state and its start. A consumer drops a
/// message whose <see cref="StartedAt"/> is not after the last one it stored.
/// </summary>
public sealed record MachineStateChanged : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:MachineStateChanged";

    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }

    public required MachineStateValue State { get; init; }

    public required DateTime StartedAt { get; init; }
}

public class MachineStateChangedValidator : AbstractValidator<MachineStateChanged>
{
    public MachineStateChangedValidator()
    {
        RuleFor(message => message.MachineId)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.MachineIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.State)
            .NotNull()
            .WithMessage(MachineMessageErrors.Validation.StateRequired);

        RuleFor(message => message.StartedAt)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.StartedAtRequired);
    }
}
