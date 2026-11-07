using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Telemetry.Application.MachineRegistrations;

/// <summary>
/// The copy of the Core contract <c>core.v1:MachineDeleted</c>. The message type name and the fields equal the fields
/// of the Core record, and a contract test checks it.
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
            .WithMessage(MachineRegistrationErrors.MachineIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(MachineRegistrationErrors.CompanyIdRequired);
    }
}
