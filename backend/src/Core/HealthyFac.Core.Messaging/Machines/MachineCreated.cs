using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Core.Messaging.Machines;

/// <summary>
/// Tells the Telemetry service that a machine exists. The machine and its gateway ID never change, so one message is
/// enough.
/// </summary>
public sealed record MachineCreated : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:MachineCreated";

    public required Guid MachineId { get; init; }

    public required string GatewayId { get; init; }

    public required Guid CompanyId { get; init; }
}

public class MachineCreatedValidator : AbstractValidator<MachineCreated>
{
    public MachineCreatedValidator()
    {
        RuleFor(message => message.MachineId)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.MachineIdRequired);

        RuleFor(message => message.GatewayId)
            .SingleLineText()
            .Must(GatewayIds.IsValid)
            .WithMessage(MachineMessageErrors.Validation.GatewayIdInvalid);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.CompanyIdRequired);
    }
}
