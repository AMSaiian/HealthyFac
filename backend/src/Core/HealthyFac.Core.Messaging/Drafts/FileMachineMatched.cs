using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.Drafts;

/// <summary>
/// Tells Document that the serial number in a company file matched exactly one machine. Document ties the file to
/// the machine, unless a user tied or untied it after <see cref="QueuedAt"/>.
/// </summary>
public sealed record FileMachineMatched : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:FileMachineMatched";

    public required Guid FileId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }

    /// <summary>The time when the user queued the ingest that found the match.</summary>
    public required DateTime QueuedAt { get; init; }
}

public class FileMachineMatchedValidator : AbstractValidator<FileMachineMatched>
{
    public FileMachineMatchedValidator()
    {
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
