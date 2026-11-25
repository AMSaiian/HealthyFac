using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Drafts;

public sealed class DraftsProposedPreHandlerCheck : PassThroughPreHandlerCheck<DraftsProposed>;

/// <summary>
/// Stores the machines that the extraction of a file proposed, as pending machine drafts (Telemetry stores the metrics
/// and statuses), and tells Telemetry how the proposal was resolved in the same save. An entry with a target machine,
/// or (for a company file) with a serial number that matches one active machine of the company, is an update
/// proposal. Any other entry is a create proposal. A proposal outside the company of the actor, or with a target that
/// is not an active machine of the company, stores nothing and commits a rejected <see cref="DraftsResolved"/>, so the
/// agent output never reaches another company and Telemetry removes the waiting definition drafts. Every draft keeps
/// the ID of the message, so a repeat delivery stores no second draft and sends no second resolution of a stored
/// proposal. One save stores all drafts and the optional <see cref="FileMachineMatched"/> message.
/// </summary>
/// <param name="dbContext">Reads the machines and saves the drafts.</param>
/// <param name="outbox">Stores the match and resolution messages with the drafts.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class StoreProposedDraftsRequestHandler(
    ICoreDbContext dbContext,
    IOutbox outbox,
    ICurrentUserProvider currentUser
) : RequestHandler<DraftsProposed>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    protected override async Task HandleInternalAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        var isInScope = await IsInScopeAsync(request, cancellationToken);

        if (!isInScope && request.Machines.Count == 0)
        {
            return;
        }

        if (!isInScope)
        {
            _outbox.Add(
                ToResolution(
                    request,
                    DraftResolution.Rejected,
                    [.. request.Machines.Select(entry => new ResolvedEntry { EntryId = entry.DraftId })]));

            await _dbContext.SaveChangesAsync(cancellationToken);

            return;
        }

        var existingIds = await LoadExistingIdsAsync(request, cancellationToken);
        var isRepeat = existingIds.Count > 0;
        await AddProposalAsync(request, cancellationToken);
        var resolvedEntries = new List<ResolvedEntry>();

        foreach (var entry in request.Machines)
        {
            var target = await ResolveTargetAsync(request, entry, cancellationToken);
            AddMachineDraft(request, entry, target, existingIds);

            resolvedEntries.Add(
                new ResolvedEntry
                {
                    EntryId = entry.DraftId,
                    MachineId = target?.Id,
                    IsOnMachineDraft = target is null,
                });

            var isSingleSerialMatch = IsIngest(request)
                && request.IsMachineFile is false
                && request.Machines.Count == 1
                && entry.TargetMachineId is null
                && target is not null;

            if (isSingleSerialMatch && !isRepeat)
            {
                _outbox.Add(
                    new FileMachineMatched
                    {
                        FileId = request.FileId!.Value,
                        MachineId = target!.Id,
                        CompanyId = request.CompanyId,
                        QueuedAt = request.QueuedAt!.Value,
                    });
            }
        }

        if (!isRepeat && request.Machines.Count > 0)
        {
            _outbox.Add(ToResolution(request, DraftResolution.Accepted, resolvedEntries));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsIngest(DraftsProposed request) => request.Source == ProposalSource.Ingest;

    private static Guid ProposalIdOf(DraftsProposed request) =>
        (IsIngest(request) ? request.AttemptId : request.ChatMessageId)!.Value;

    private static DraftsResolved ToResolution(
        DraftsProposed request,
        DraftResolution resolution,
        IReadOnlyList<ResolvedEntry> entries
    ) =>
        new()
        {
            Source = IsIngest(request) ? ResolutionSource.Ingest : ResolutionSource.Chat,
            AttemptId = request.AttemptId,
            CompanyId = request.CompanyId,
            Resolution = resolution,
            Entries = entries,
        };

    private async Task<bool> IsInScopeAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        if (_currentUser.GetActor().CompanyId != request.CompanyId)
        {
            return false;
        }

        var targetIds = request.Machines
            .Select(machine => machine.TargetMachineId)
            .OfType<Guid>()
            .Distinct()
            .ToList();

        var foundCount = await _dbContext.Machines
            .Where(machine => machine.CompanyId == request.CompanyId && targetIds.Contains(machine.Id))
            .CountAsync(cancellationToken);

        return foundCount == targetIds.Count;
    }

    private async Task AddProposalAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        var isProposalStored = await _dbContext.DraftProposals
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AnyAsync(proposal => proposal.Id == ProposalIdOf(request), cancellationToken);

        if (isProposalStored)
        {
            return;
        }

        _dbContext.DraftProposals.Add(
            IsIngest(request)
                ? DraftProposal.ForIngest(
                    ProposalIdOf(request), request.CompanyId, request.FileId!.Value, request.QueuedAt!.Value)
                : DraftProposal.ForChat(ProposalIdOf(request), request.CompanyId, request.ChatId!.Value));
    }

    private async Task<HashSet<Guid>> LoadExistingIdsAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        var machineDraftIds = request.Machines.Select(machine => machine.DraftId).ToList();

        var existing = await _dbContext.MachineDrafts
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(draft => machineDraftIds.Contains(draft.Id))
            .Select(draft => draft.Id)
            .ToListAsync(cancellationToken);

        return [.. existing];
    }

    private async Task<TargetMachine?> ResolveTargetAsync(
        DraftsProposed request,
        ProposedMachine entry,
        CancellationToken cancellationToken
    )
    {
        if (entry.TargetMachineId is { } targetId)
        {
            return await _dbContext.Machines
                .AsNoTracking()
                .Where(machine => machine.Id == targetId && machine.CompanyId == request.CompanyId)
                .Select(machine => new TargetMachine { Id = machine.Id, SerialNumber = machine.SerialNumber })
                .SingleOrDefaultAsync(cancellationToken);
        }

        if (!IsIngest(request) || request.IsMachineFile is true || entry.SerialNumber is null)
        {
            return null;
        }

        var serialNumber = entry.SerialNumber;

        return await _dbContext.Machines
            .AsNoTracking()
            .Where(machine => machine.CompanyId == request.CompanyId && machine.SerialNumber == serialNumber)
            .Select(machine => new TargetMachine { Id = machine.Id, SerialNumber = machine.SerialNumber })
            .SingleOrDefaultAsync(cancellationToken);
    }

    private void AddMachineDraft(
        DraftsProposed request,
        ProposedMachine entry,
        TargetMachine? target,
        HashSet<Guid> existingIds
    )
    {
        var proposedSerialNumber = target is null
            ? entry.SerialNumber
            : string.Equals(entry.SerialNumber, target.SerialNumber, StringComparison.OrdinalIgnoreCase)
                ? null
                : entry.SerialNumber;

        var hasProposal = target is null
            || entry.Name is not null
            || entry.Description is not null
            || proposedSerialNumber is not null;

        if (hasProposal && !existingIds.Contains(entry.DraftId))
        {
            _dbContext.MachineDrafts.Add(
                new MachineDraft(
                    entry.DraftId,
                    request.CompanyId,
                    ProposalIdOf(request),
                    target?.Id,
                    entry.Name,
                    proposedSerialNumber,
                    entry.Description));
        }
    }

    private sealed record TargetMachine
    {
        public required Guid Id { get; init; }

        public required string SerialNumber { get; init; }
    }
}
