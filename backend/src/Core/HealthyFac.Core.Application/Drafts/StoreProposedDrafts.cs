using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Drafts;

/// <summary>
/// Checks that the proposal belongs to the company of the restored actor (a mismatch is a <c>403</c>), and that every
/// target machine of the message is an active machine of that company (a miss is a <c>404</c>). The checks keep the
/// agent output from reaching another company, even when the message holds a wrong ID.
/// </summary>
/// <param name="dbContext">Reads the target machines.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class DraftsProposedPreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DraftsProposed>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        var targetIds = request.Machines
            .Select(machine => machine.TargetMachineId)
            .OfType<Guid>()
            .Distinct()
            .ToList();
        var foundCount = await _dbContext.Machines
            .Where(machine => machine.CompanyId == request.CompanyId && targetIds.Contains(machine.Id))
            .CountAsync(cancellationToken);

        if (foundCount != targetIds.Count)
        {
            throw new NotFoundException(DraftErrors.TargetMachineNotFound);
        }
    }
}

/// <summary>
/// Stores the machines, metrics, and statuses that the extraction of a file proposed, as pending drafts. An entry
/// with a target machine, or (for a company file) with a serial number that matches one active machine of the
/// company, is an update proposal. Any other entry is a create proposal, and its metrics and statuses wait on the
/// create draft until a user applies it. Every draft keeps the ID of the message, so a repeat delivery stores
/// nothing new. One save stores all drafts and the optional <see cref="FileMachineMatched"/> message.
/// </summary>
/// <param name="dbContext">Reads the machines and saves the drafts.</param>
/// <param name="outbox">Stores the match message with the drafts.</param>
public sealed class StoreProposedDraftsRequestHandler(ICoreDbContext dbContext, IOutbox outbox)
    : RequestHandler<DraftsProposed>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;

    protected override async Task HandleInternalAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        var existingIds = await LoadExistingIdsAsync(request, cancellationToken);
        var isRepeat = existingIds.Count > 0;

        foreach (var entry in request.Machines)
        {
            var target = await ResolveTargetAsync(request, entry, cancellationToken);
            var machineDraftId = AddMachineDraft(request, entry, target, existingIds);

            AddChildren(request, entry, target?.Id, machineDraftId, existingIds);
            var isSingleSerialMatch = !request.IsMachineFile
                && request.Machines.Count == 1
                && entry.TargetMachineId is null
                && target is not null;

            if (isSingleSerialMatch && !isRepeat)
            {
                _outbox.Add(
                    new FileMachineMatched
                    {
                        FileId = request.FileId,
                        MachineId = target!.Id,
                        CompanyId = request.CompanyId,
                        QueuedAt = request.QueuedAt,
                    });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<HashSet<Guid>> LoadExistingIdsAsync(DraftsProposed request, CancellationToken cancellationToken)
    {
        var machineDraftIds = request.Machines.Select(machine => machine.DraftId).ToList();
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
            await _dbContext.MachineDrafts
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .Where(draft => machineDraftIds.Contains(draft.Id))
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

        if (request.IsMachineFile || entry.SerialNumber is null)
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

    private Guid? AddMachineDraft(
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

        if (!hasProposal)
        {
            return null;
        }

        if (!existingIds.Contains(entry.DraftId))
        {
            _dbContext.MachineDrafts.Add(
                new MachineDraft(
                    entry.DraftId,
                    request.CompanyId,
                    request.AttemptId,
                    request.FileId,
                    request.QueuedAt,
                    target?.Id,
                    entry.Name,
                    proposedSerialNumber,
                    entry.Description));
        }

        return entry.DraftId;
    }

    private void AddChildren(
        DraftsProposed request,
        ProposedMachine entry,
        Guid? targetMachineId,
        Guid? machineDraftId,
        HashSet<Guid> existingIds
    )
    {
        foreach (var metric in entry.Metrics.Where(metric => !existingIds.Contains(metric.DraftId)))
        {
            var values = ToValues(metric);
            _dbContext.MetricDefinitionDrafts.Add(
                targetMachineId is { } machineId
                    ? MetricDefinitionDraft.ForMachine(
                        metric.DraftId, request.CompanyId, request.AttemptId, request.FileId, machineId, values)
                    : MetricDefinitionDraft.ForMachineDraft(
                        metric.DraftId,
                        request.CompanyId,
                        request.AttemptId,
                        request.FileId,
                        machineDraftId!.Value,
                        values));
        }

        foreach (var status in entry.Statuses.Where(status => !existingIds.Contains(status.DraftId)))
        {
            var values = new StatusDraftValues
            {
                SourceLocation = status.SourceLocation,
                Code = status.Code,
                Severity = status.Severity,
                Description = status.Description,
            };
            _dbContext.StatusDefinitionDrafts.Add(
                targetMachineId is { } machineId
                    ? StatusDefinitionDraft.ForMachine(
                        status.DraftId, request.CompanyId, request.AttemptId, request.FileId, machineId, values)
                    : StatusDefinitionDraft.ForMachineDraft(
                        status.DraftId,
                        request.CompanyId,
                        request.AttemptId,
                        request.FileId,
                        machineDraftId!.Value,
                        values));
        }
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

    private sealed record TargetMachine
    {
        public required Guid Id { get; init; }

        public required string SerialNumber { get; init; }
    }
}
