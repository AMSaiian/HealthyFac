using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.Drafts;

/// <summary>
/// Tells Document that a user created a machine from a draft. When the draft came from a file, Document ties the file
/// to the new machine, unless a user tied or untied it after <see cref="QueuedAt"/>.
/// </summary>
public sealed record MachineDraftApplied : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:MachineDraftApplied";

    public required Guid DraftId { get; init; }

    /// <summary>The source file, or <see langword="null"/> for a draft that a chat proposed.</summary>
    public Guid? FileId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }

    /// <summary>The time when the user queued the ingest that proposed the draft. It is set with the file.</summary>
    public DateTime? QueuedAt { get; init; }
}

public class MachineDraftAppliedValidator : AbstractValidator<MachineDraftApplied>
{
    public MachineDraftAppliedValidator()
    {
        RuleFor(message => message.DraftId)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.DraftIdRequired);

        RuleFor(message => message.FileId)
            .NotEqual(Guid.Empty)
            .When(message => message.FileId is not null)
            .WithMessage(DraftMessageErrors.Validation.FileIdEmpty);

        RuleFor(message => message)
            .Must(message => message.FileId is null == message.QueuedAt is null)
            .WithMessage(DraftMessageErrors.Validation.FileAndQueuedAtTogether);

        RuleFor(message => message.MachineId)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.MachineIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.QueuedAt)
            .NotEqual(default(DateTime))
            .When(message => message.QueuedAt is not null)
            .WithMessage(DraftMessageErrors.Validation.QueuedAtEmpty);
    }
}
