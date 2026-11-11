using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.Machines;

/// <summary>
/// Tells the services that keep a copy of the machine name (Document) that the name changed.
/// </summary>
public sealed record MachineRenamed : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:MachineRenamed";

    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }

    public required string Name { get; init; }

    public required DateTime RenamedAt { get; init; }
}

public class MachineRenamedValidator : AbstractValidator<MachineRenamed>
{
    public MachineRenamedValidator()
    {
        RuleFor(message => message.MachineId)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.MachineIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.Name)
            .SingleLineText()
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.NameRequired)
            .MaximumLength(MachineMessageLimits.NameMaxLength)
            .WithMessage(MachineMessageErrors.Validation.NameTooLong);

        RuleFor(message => message.RenamedAt)
            .NotEmpty()
            .WithMessage(MachineMessageErrors.Validation.RenamedAtRequired);
    }
}
