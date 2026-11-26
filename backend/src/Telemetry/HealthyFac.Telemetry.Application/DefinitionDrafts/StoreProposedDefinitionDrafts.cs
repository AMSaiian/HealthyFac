using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application.DefinitionDrafts.Following;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Application.Realtime;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.StatusDrafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
/// Stores the metrics, statuses, and phases of a proposal (an ingest or a chat) as definition drafts. A definition with
/// a <c>MachineId</c> belongs to a machine that exists: its draft is stored <c>pending</c> on that machine, with no
/// proposal entry. The registration is locked first. A soft-deleted registration stores nothing and logs a warning. An
/// unknown registration throws <see cref="NotFoundException"/>, so the inbox retries the message. A definition with a
/// <c>MachineDraftId</c> belongs to a new machine of the same message: its draft is stored <c>awaitingTarget</c> on the
/// proposal entry of that machine draft. When the outcome of the machine check arrived first, a new draft follows its
/// entry at once. Every draft keeps the ID of the message, so a repeat delivery stores nothing new, also after a soft
/// delete. The save checks the row version of every entry that already exists, so a parallel message that changes the
/// entry makes one save fail and the inbox retries it. The handler locks the machines of the definitions and of the
/// entries in one ascending machine ID pass, so 2 messages lock their registrations in one order.
/// </summary>
/// <param name="dbContext">Reads the stored drafts and saves the new ones.</param>
/// <param name="proposalEntries">Finds the entries and applies their state to the new drafts.</param>
/// <param name="registrationLock">Locks the registration of a machine before the handler reads it.</param>
/// <param name="notifier">Adds the hint of the stored drafts. The inbox runner pushes it after the commit.</param>
/// <param name="logger">Logs a definition that targets a deleted machine.</param>
public sealed class StoreProposedDefinitionDraftsRequestHandler(
    ITelemetryDbContext dbContext,
    ProposalEntries proposalEntries,
    IMachineRegistrationLock registrationLock,
    IRealtimeNotifier notifier,
    ILogger<StoreProposedDefinitionDraftsRequestHandler> logger
) : RequestHandler<DraftsProposed>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ProposalEntries _proposalEntries = proposalEntries;
    private readonly IMachineRegistrationLock _registrationLock = registrationLock;
    private readonly IRealtimeNotifier _notifier = notifier;
    private readonly ILogger<StoreProposedDefinitionDraftsRequestHandler> _logger = logger;

    protected override async Task HandleInternalAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        var existingIds = await LoadExistingIdsAsync(request, cancellationToken);
        await AddProposalAsync(request, cancellationToken);

        var storedEntries = await FindEntriesAsync(request, cancellationToken);
        var registrations = await LockRegistrationsAsync(request, storedEntries, cancellationToken);
        var activeMachineIds = FindActiveMachineIds(request, registrations);
        var entries = ToEntryStates(storedEntries, registrations);
        var proposalId = ProposalIdOf(request);
        var hintedMachineIds = new HashSet<Guid>();

        foreach (var metric in request.Metrics.Where(metric => !existingIds.Contains(metric.DraftId)))
        {
            var draft = metric.MachineId is { } machineId
                ? activeMachineIds.Contains(machineId)
                    ? MetricDefinitionDraft.ProposeOnMachine(
                        metric.DraftId, request.CompanyId, proposalId, machineId, ToValues(metric))
                    : null
                : ProposeOnEntry(
                    entries[metric.MachineDraftId!.Value],
                    entry => MetricDefinitionDraft.Propose(
                        metric.DraftId, request.CompanyId, proposalId, entry.Id, tentativeMachineId: null,
                        ToValues(metric)));

            if (draft is not null)
            {
                _dbContext.MetricDefinitionDrafts.Add(draft);

                if (metric.MachineId is { } metricMachineId)
                {
                    hintedMachineIds.Add(metricMachineId);
                }
            }
        }

        foreach (var status in request.Statuses.Where(status => !existingIds.Contains(status.DraftId)))
        {
            var draft = status.MachineId is { } machineId
                ? activeMachineIds.Contains(machineId)
                    ? StatusDefinitionDraft.ProposeOnMachine(
                        status.DraftId, request.CompanyId, proposalId, machineId, ToValues(status))
                    : null
                : ProposeOnEntry(
                    entries[status.MachineDraftId!.Value],
                    entry => StatusDefinitionDraft.Propose(
                        status.DraftId, request.CompanyId, proposalId, entry.Id, tentativeMachineId: null,
                        ToValues(status)));

            if (draft is not null)
            {
                _dbContext.StatusDefinitionDrafts.Add(draft);

                if (status.MachineId is { } statusMachineId)
                {
                    hintedMachineIds.Add(statusMachineId);
                }
            }
        }

        foreach (var phase in request.Phases.Where(phase => !existingIds.Contains(phase.DraftId)))
        {
            var draft = phase.MachineId is { } machineId
                ? activeMachineIds.Contains(machineId)
                    ? OperatingPhaseDraft.ProposeOnMachine(
                        phase.DraftId,
                        request.CompanyId,
                        proposalId,
                        machineId,
                        ToValues(phase),
                        [.. phase.Conditions.Select(ToValues)],
                        [.. phase.Limits.Select(ToValues)])
                    : null
                : ProposeOnEntry(
                    entries[phase.MachineDraftId!.Value],
                    entry => OperatingPhaseDraft.Propose(
                        phase.DraftId,
                        request.CompanyId,
                        proposalId,
                        entry.Id,
                        tentativeMachineId: null,
                        ToValues(phase),
                        [.. phase.Conditions.Select(ToValues)],
                        [.. phase.Limits.Select(ToValues)]));

            if (draft is not null)
            {
                _dbContext.OperatingPhaseDrafts.Add(draft);

                if (phase.MachineId is { } phaseMachineId)
                {
                    hintedMachineIds.Add(phaseMachineId);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var machineId in hintedMachineIds)
        {
            _notifier.AddDefinitionDraftsHint(request.CompanyId, request.AttemptId, machineId);
        }
    }

    private static Guid ProposalIdOf(DraftsProposed request) =>
        (request.Source == ProposalSource.Ingest ? request.AttemptId : request.ChatMessageId)!.Value;

    private static TDraft? ProposeOnEntry<TDraft>(EntryState state, Func<ProposalEntry, TDraft> propose)
        where TDraft : class, IDefinitionDraft
    {
        var draft = propose(state.Entry);
        var isRemovalRequired = ProposalEntries.FollowNewDraft(draft, state.Entry, state.IsTargetRegistrationDeleted);

        return isRemovalRequired ? null : draft;
    }

    private async Task<List<ProposalEntry>> FindEntriesAsync(
        DraftsProposed request,
        CancellationToken cancellationToken
    )
    {
        var machineDraftIds = request.Metrics.Select(metric => metric.MachineDraftId)
            .Concat(request.Statuses.Select(status => status.MachineDraftId))
            .Concat(request.Phases.Select(phase => phase.MachineDraftId))
            .OfType<Guid>()
            .Distinct()
            .ToList();

        var found = new List<ProposalEntry>();

        foreach (var machineDraftId in machineDraftIds)
        {
            var entry = await _proposalEntries.FindOrCreateAsync(machineDraftId, request.CompanyId, cancellationToken);
            _proposalEntries.EnsureRowVersionChecked(entry);
            found.Add(entry);
        }

        return found;
    }

    /// <summary>
    /// Locks every registration that the message reads in one ascending machine ID pass: the machines of the
    /// definitions and the machines that the entries already name. It reads each registration after its lock, also a
    /// soft-deleted one.
    /// </summary>
    /// <param name="request">The message. Its definitions name the machines.</param>
    /// <param name="entries">The proposal entries of the message. An entry can already name a machine.</param>
    /// <param name="cancellationToken">Cancels the locks and the reads.</param>
    /// <returns>
    /// A map from the machine ID to <see langword="true"/> when its registration is soft-deleted. An entry machine
    /// without a registration has no key.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// The registration of a machine that a definition names is unknown, or belongs to another company.
    /// </exception>
    private async Task<Dictionary<Guid, bool>> LockRegistrationsAsync(
        DraftsProposed request,
        List<ProposalEntry> entries,
        CancellationToken cancellationToken
    )
    {
        var definitionMachineIds = DefinitionMachineIdsOf(request);

        var machineIds = definitionMachineIds
            .Concat(entries.Select(entry => entry.MachineId).OfType<Guid>())
            .Distinct()
            .Order()
            .ToList();

        var isDeletedByMachineId = new Dictionary<Guid, bool>();

        foreach (var machineId in machineIds)
        {
            await _registrationLock.LockLiveAsync(machineId, cancellationToken);

            var registration = await _dbContext.MachineRegistrations
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .Where(candidate => candidate.Id == machineId)
                .Select(candidate => new { candidate.CompanyId, candidate.DeletedAt })
                .SingleOrDefaultAsync(cancellationToken);

            var isDefinitionMachine = definitionMachineIds.Contains(machineId);

            if (isDefinitionMachine && (registration is null || registration.CompanyId != request.CompanyId))
            {
                throw new NotFoundException(DefinitionDraftErrors.MachineNotRegistered);
            }

            if (registration is null)
            {
                continue;
            }

            isDeletedByMachineId[machineId] = registration.DeletedAt is not null;

            if (isDefinitionMachine && registration.DeletedAt is not null)
            {
                _logger.LogWarning(
                    message: "The proposal of definition drafts targets a deleted machine, so it stores nothing.");
            }
        }

        return isDeletedByMachineId;
    }

    private static HashSet<Guid> FindActiveMachineIds(
        DraftsProposed request,
        Dictionary<Guid, bool> isDeletedByMachineId
    ) =>
        [.. DefinitionMachineIdsOf(request).Where(machineId => !isDeletedByMachineId[machineId])];

    private static HashSet<Guid> DefinitionMachineIdsOf(DraftsProposed request) =>
    [
        .. request.Metrics.Select(metric => metric.MachineId)
            .Concat(request.Statuses.Select(status => status.MachineId))
            .Concat(request.Phases.Select(phase => phase.MachineId))
            .OfType<Guid>(),
    ];

    private static Dictionary<Guid, EntryState> ToEntryStates(
        List<ProposalEntry> entries,
        Dictionary<Guid, bool> isDeletedByMachineId
    ) =>
        entries.ToDictionary(
            entry => entry.Id,
            entry => new EntryState
            {
                Entry = entry,
                IsTargetRegistrationDeleted = entry.MachineId is { } machineId
                    && isDeletedByMachineId.TryGetValue(machineId, out var isDeleted)
                    && isDeleted,
            });

    private async Task AddProposalAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        var proposalId = ProposalIdOf(request);

        var isProposalStored = await _dbContext.DraftProposals
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AnyAsync(proposal => proposal.Id == proposalId, cancellationToken);

        if (isProposalStored)
        {
            return;
        }

        _dbContext.DraftProposals.Add(
            request.Source == ProposalSource.Ingest
                ? DraftProposal.ForIngest(proposalId, request.CompanyId, request.FileId!.Value)
                : DraftProposal.ForChat(
                    proposalId, request.CompanyId, request.ChatId!.Value, request.RequestedByName));
    }

    private async Task<HashSet<Guid>> LoadExistingIdsAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        var metricIds = request.Metrics.Select(metric => metric.DraftId).ToList();
        var statusIds = request.Statuses.Select(status => status.DraftId).ToList();
        var phaseIds = request.Phases.Select(phase => phase.DraftId).ToList();

        var existing = new HashSet<Guid>();

        existing.UnionWith(
            await _dbContext.OperatingPhaseDrafts
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .Where(draft => phaseIds.Contains(draft.Id))
                .Select(draft => draft.Id)
                .ToListAsync(cancellationToken));

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

    private sealed record EntryState
    {
        public required ProposalEntry Entry { get; init; }

        public required bool IsTargetRegistrationDeleted { get; init; }
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
            WarningLow = metric.WarningLow,
            WarningHigh = metric.WarningHigh,
            AlarmLow = metric.AlarmLow,
            AlarmHigh = metric.AlarmHigh,
            Behavior = metric.Behavior,
        };

    private static PhaseDraftValues ToValues(ProposedPhase phase) =>
        new()
        {
            SourceLocation = phase.SourceLocation,
            Code = phase.Code,
            Name = phase.Name,
            Description = phase.Description,
            Priority = phase.Priority,
            IsTracked = phase.IsTracked,
        };

    private static PhaseDraftConditionValues ToValues(ProposedPhaseCondition condition) =>
        new()
        {
            MetricDefinitionId = condition.MetricId,
            MetricDraftId = condition.MetricDraftId,
            StatusDefinitionId = condition.StatusId,
            StatusDraftId = condition.StatusDraftId,
            Operator = condition.Operator,
            Value = condition.Value,
            UpperValue = condition.UpperValue,
            States = condition.States,
        };

    private static PhaseDraftLimitValues ToValues(ProposedPhaseLimit limit) =>
        new()
        {
            MetricDefinitionId = limit.MetricId,
            MetricDraftId = limit.MetricDraftId,
            WarningLow = limit.WarningLow,
            WarningHigh = limit.WarningHigh,
            AlarmLow = limit.AlarmLow,
            AlarmHigh = limit.AlarmHigh,
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
