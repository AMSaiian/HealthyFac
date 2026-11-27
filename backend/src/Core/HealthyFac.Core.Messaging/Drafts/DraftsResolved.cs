using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.Drafts;

/// <summary>
/// Tells Telemetry how a user resolved the entries of a proposal. Each entry is one proposed machine. Telemetry moves
/// the definition drafts of an entry to its target, or removes the waiting ones when the user rejected it.
/// </summary>
public sealed record DraftsResolved : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:DraftsResolved";

    public required ResolutionSource Source { get; init; }

    /// <summary>The ingest attempt of an <c>ingest</c> proposal. A <c>chat</c> proposal has none.</summary>
    public Guid? AttemptId { get; init; }

    public required Guid CompanyId { get; init; }

    public required DraftResolution Resolution { get; init; }

    public required IReadOnlyList<ResolvedEntry> Entries { get; init; }
}

/// <summary>
/// One resolved entry. An accepted entry has exactly one target: <see cref="MachineId"/> (an existing machine) or
/// <see cref="IsOnMachineDraft"/> (a create machine draft with the entry ID). A rejected entry has neither.
/// </summary>
public sealed record ResolvedEntry
{
    public required Guid EntryId { get; init; }

    public Guid? MachineId { get; init; }

    public bool IsOnMachineDraft { get; init; }
}

public class DraftsResolvedValidator : AbstractValidator<DraftsResolved>
{
    public DraftsResolvedValidator()
    {
        RuleFor(message => message.Source)
            .NotNull()
            .WithMessage(DraftMessageErrors.Validation.SourceRequired);

        RuleFor(message => message.AttemptId)
            .MustBeSetId()
            .When(message => message.Source == ResolutionSource.Ingest)
            .WithMessage(DraftMessageErrors.Validation.AttemptIdRequired);

        RuleFor(message => message.AttemptId)
            .Null()
            .When(message => message.Source == ResolutionSource.Chat)
            .WithMessage(DraftMessageErrors.Validation.AttemptIdOnlyForIngest);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.Resolution)
            .NotNull()
            .WithMessage(DraftMessageErrors.Validation.ResolutionRequired);

        RuleFor(message => message.Entries)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.EntriesRequired);

        RuleForEach(message => message.Entries)
            .Must(entry => entry.EntryId != Guid.Empty)
            .WithMessage(DraftMessageErrors.Validation.EntryIdRequired);

        RuleForEach(message => message.Entries)
            .Must(HaveExactlyOneTarget)
            .When(message => message.Resolution == DraftResolution.Accepted)
            .WithMessage(DraftMessageErrors.Validation.AcceptedEntryNeedsOneTarget);

        RuleForEach(message => message.Entries)
            .Must(HaveNoTarget)
            .When(message => message.Resolution == DraftResolution.Rejected)
            .WithMessage(DraftMessageErrors.Validation.RejectedEntryHasNoTarget);
    }

    private static bool HaveExactlyOneTarget(ResolvedEntry entry) =>
        entry.MachineId is not null != entry.IsOnMachineDraft;

    private static bool HaveNoTarget(ResolvedEntry entry) =>
        entry.MachineId is null && !entry.IsOnMachineDraft;
}
