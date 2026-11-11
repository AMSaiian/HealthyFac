using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Document.Messaging.Files;

/// <summary>
/// Tells the services that hold data of a file (Insights) that the machine of the file changed. A null machine
/// means that the file is not tied to a machine.
/// </summary>
public sealed record FileMachineChanged : IIntegrationMessage, IRequest
{
    public static string MessageType => "document.v1:FileMachineChanged";

    public required Guid FileId { get; init; }

    public required Guid CompanyId { get; init; }

    public Guid? MachineId { get; init; }

    /// <summary>The time of the change. A receiver ignores a change that is older than the one it holds.</summary>
    public required DateTime ChangedAt { get; init; }
}

public class FileMachineChangedValidator : AbstractValidator<FileMachineChanged>
{
    public FileMachineChangedValidator()
    {
        RuleFor(message => message.FileId)
            .NotEmpty()
            .WithMessage(FileMessageErrors.Validation.FileIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(FileMessageErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.MachineId)
            .NotEqual(Guid.Empty)
            .When(message => message.MachineId is not null)
            .WithMessage(FileMessageErrors.Validation.MachineIdInvalid);

        RuleFor(message => message.ChangedAt)
            .NotEmpty()
            .WithMessage(FileMessageErrors.Validation.ChangedAtRequired);
    }
}
