using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.Machines;

/// <summary>
/// Tells the Telemetry service that a machine is deleted, so it removes the registration, the metric definitions,
/// and the status definitions of the machine.
/// </summary>
public sealed record MachineDeleted : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:MachineDeleted";

    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }
}

public class MachineDeletedValidator : AbstractValidator<MachineDeleted>
{
    public MachineDeletedValidator()
    {
        RuleFor(message => message.MachineId)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.MachineIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.CompanyIdRequired);
    }
}
