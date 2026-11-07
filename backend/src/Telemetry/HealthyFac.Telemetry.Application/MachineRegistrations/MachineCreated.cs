using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Telemetry.Application.MachineRegistrations;

/// <summary>
/// The copy of the Core contract <c>core.v1:MachineCreated</c>. The message type name and the fields equal the
/// fields of the Core record, and a contract test checks it. There is no project reference between the services.
/// </summary>
public sealed record MachineCreated : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:MachineCreated";

    public required Guid MachineId { get; init; }

    public required string GatewayId { get; init; }

    public required Guid CompanyId { get; init; }

    public required Guid DepartmentId { get; init; }
}

public class MachineCreatedValidator : AbstractValidator<MachineCreated>
{
    public MachineCreatedValidator()
    {
        RuleFor(message => message.MachineId)
            .NotEmpty()
            .WithMessage(MachineRegistrationErrors.MachineIdRequired);

        RuleFor(message => message.GatewayId)
            .SingleLineText()
            .Must(GatewayIds.IsValid)
            .WithMessage(MachineRegistrationErrors.GatewayIdInvalid);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(MachineRegistrationErrors.CompanyIdRequired);

        RuleFor(message => message.DepartmentId)
            .NotEmpty()
            .WithMessage(MachineRegistrationErrors.DepartmentIdRequired);
    }
}
