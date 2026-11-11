using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.Drafts;

/// <summary>
/// Tells Document that a user created a machine from a draft that came from a file. Document ties the file to the
/// new machine, unless a user tied or untied it after <see cref="QueuedAt"/>.
/// </summary>
public sealed record MachineDraftApplied : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:MachineDraftApplied";

    public required Guid DraftId { get; init; }

    public required Guid FileId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }

    /// <summary>The time when the user queued the ingest that proposed the draft.</summary>
    public required DateTime QueuedAt { get; init; }
}

public class MachineDraftAppliedValidator : AbstractValidator<MachineDraftApplied>
{
    public MachineDraftAppliedValidator()
    {
        RuleFor(message => message.DraftId)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.DraftIdRequired);

        RuleFor(message => message.FileId)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.FileIdRequired);

        RuleFor(message => message.MachineId)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.MachineIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.QueuedAt)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.QueuedAtRequired);
    }
}
