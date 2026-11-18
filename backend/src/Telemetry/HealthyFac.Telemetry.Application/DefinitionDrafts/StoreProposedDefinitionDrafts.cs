using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

/// <summary>
/// Checks that the proposal belongs to the company of the restored actor (a mismatch is a <c>403</c>).
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class StoreProposedDefinitionDraftsPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DraftsProposed>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Stores the metrics and statuses that the extraction of a file proposed, as definition drafts in the state
/// <c>awaitingTarget</c>. Each proposed machine is one proposal entry. When the outcome of the machine check arrived
/// first, a new draft follows its entry at once. Every draft keeps the ID of the message, so a repeat delivery
/// stores nothing new, also after a soft delete. The save checks the row version of every entry that already exists,
/// so a parallel message that changes the entry makes one save fail and the inbox retries it. The handler follows the
/// entries in ascending machine ID order, so 2 messages lock their registrations in one order.
/// </summary>
/// <param name="dbContext">Reads the stored drafts and saves the new ones.</param>
/// <param name="proposalEntries">Finds the entries and applies their state to the new drafts.</param>
public sealed class StoreProposedDefinitionDraftsRequestHandler(
    ITelemetryDbContext dbContext,
    ProposalEntries proposalEntries
) : RequestHandler<DraftsProposed>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ProposalEntries _proposalEntries = proposalEntries;

    protected override async Task HandleInternalAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        var existingIds = await LoadExistingIdsAsync(request, cancellationToken);

        var proposals = new List<(ProposedMachine Proposed, ProposalEntry Entry)>();

        foreach (var machine in request.Machines)
        {
            var found = await _proposalEntries.FindOrCreateAsync(
                machine.DraftId, request.CompanyId, cancellationToken);
            _proposalEntries.EnsureRowVersionChecked(found);
            proposals.Add((machine, found));
        }

        foreach (var (proposed, entry) in proposals.OrderBy(proposal => proposal.Entry.MachineId))
        {
            var isTargetRegistrationDeleted = await _proposalEntries.IsTargetRegistrationDeletedAsync(
                entry, cancellationToken);

            foreach (var metric in proposed.Metrics.Where(metric => !existingIds.Contains(metric.DraftId)))
            {
                var draft = MetricDefinitionDraft.Propose(
                    metric.DraftId,
                    request.CompanyId,
                    request.AttemptId,
                    request.FileId,
                    entry.Id,
                    proposed.TargetMachineId,
                    ToValues(metric));
                var isRemovalRequired = ProposalEntries.FollowNewDraft(draft, entry, isTargetRegistrationDeleted);

                if (!isRemovalRequired)
                {
                    _dbContext.MetricDefinitionDrafts.Add(draft);
                }
            }

            foreach (var status in proposed.Statuses.Where(status => !existingIds.Contains(status.DraftId)))
            {
                var draft = StatusDefinitionDraft.Propose(
                    status.DraftId,
                    request.CompanyId,
                    request.AttemptId,
                    request.FileId,
                    entry.Id,
                    proposed.TargetMachineId,
                    ToValues(status));
                var isRemovalRequired = ProposalEntries.FollowNewDraft(draft, entry, isTargetRegistrationDeleted);

                if (!isRemovalRequired)
                {
                    _dbContext.StatusDefinitionDrafts.Add(draft);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<HashSet<Guid>> LoadExistingIdsAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        var metricIds = request.Machines
            .SelectMany(machine => machine.Metrics)
            .Select(metric => metric.DraftId)
            .ToList();
        var statusIds = request.Machines
            .SelectMany(machine => machine.Statuses)
            .Select(status => status.DraftId)
            .ToList();

        var existing = new HashSet<Guid>();
        existing.UnionWith(
            await _dbContext.MetricDefinitionDrafts
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .Where(draft => metricIds.Contains(draft.Id))
                .Select(draft => draft.Id)
                .ToListAsync(cancellationToken));
        existing.UnionWith(
            await _dbContext.StatusDefinitionDrafts
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .Where(draft => statusIds.Contains(draft.Id))
                .Select(draft => draft.Id)
                .ToListAsync(cancellationToken));

        return existing;
    }

    private static MetricDraftValues ToValues(ProposedMetric metric) =>
        new()
        {
            SourceLocation = metric.SourceLocation,
            Key = metric.Key,
            Name = metric.Name,
            Description = metric.Description,
            ValueKind = metric.ValueKind,
            QuantityKindName = metric.QuantityKind,
            UnitName = metric.Unit,
            Min = metric.Min,
            Max = metric.Max,
            States = metric.States,
        };

    private static StatusDraftValues ToValues(ProposedStatus status) =>
        new()
        {
            SourceLocation = status.SourceLocation,
            Code = status.Code,
            Severity = status.Severity,
            Description = status.Description,
        };
}
