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

    public required string Name { get; init; }

    /// <summary>The expected go-live day in the company time zone.</summary>
    public required DateOnly ExpectedGoLiveOn { get; init; }

    /// <summary>
    /// The UTC instant of the local midnight of <see cref="ExpectedGoLiveOn"/> in the company time zone. A midnight
    /// in a skipped hour moves forward by the gap length.
    /// </summary>
    public required DateTime GoLiveStartsAt { get; init; }
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

        RuleFor(message => message.Name)
            .SingleLineText()
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.NameRequired)
            .MaximumLength(MachineMessageLimits.NameMaxLength)
            .WithMessage(MachineMessageErrors.Validation.NameTooLong);
    }
}
