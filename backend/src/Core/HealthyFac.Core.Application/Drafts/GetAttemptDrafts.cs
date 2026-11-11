using FluentValidation;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Drafts;

public record GetAttemptDraftsRequest : IRequest<AttemptDraftsResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid AttemptId { get; init; }
}

public record AttemptDraftsResponse
{
    public required IReadOnlyList<AttemptDraftGroup> Groups { get; init; }
}

/// <summary>
/// The drafts of one attempt that belong to one machine: an existing machine (the target, or the machine that a create
/// draft made), or the machine that a pending create draft proposes.
/// </summary>
public record AttemptDraftGroup
{
    /// <summary>The target or created machine, or <see langword="null"/> for a pending create draft.</summary>
    public Guid? MachineId { get; init; }

    /// <summary>
    /// The current name of the machine, or <see langword="null"/> when the machine does not exist yet.
    /// </summary>
    public string? MachineName { get; init; }

    public MachineDraftDetail? MachineDraft { get; init; }

    public required IReadOnlyList<MetricDraftDetail> Metrics { get; init; }

    public required IReadOnlyList<StatusDraftDetail> Statuses { get; init; }
}

public class GetAttemptDraftsRequestValidator : AbstractValidator<GetAttemptDraftsRequest>
{
    public GetAttemptDraftsRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(DraftErrors.CompanyIdRequired);
        RuleFor(x => x.AttemptId).NotEmpty().WithMessage(DraftErrors.AttemptIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetAttemptDraftsPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetAttemptDraftsRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetAttemptDraftsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads every draft of one ingest attempt (all states) and groups them by machine. A group shows the machine draft
/// when one exists, and the metric and status drafts of the machine. The group key is the machine (the target, or the
/// machine that a create draft made) or, for a pending create draft, the draft itself. An attempt that Core does not
/// know gives an empty list, because Core does not own attempts.
/// </summary>
/// <param name="dbContext">Reads the drafts and the machine names.</param>
public sealed class GetAttemptDraftsRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<GetAttemptDraftsRequest, AttemptDraftsResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<AttemptDraftsResponse> HandleAsync(
        GetAttemptDraftsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var machineDrafts = await _dbContext.MachineDrafts
            .AsNoTracking()
            .Where(draft => draft.CompanyId == request.CompanyId && draft.AttemptId == request.AttemptId)
            .ToListAsync(cancellationToken);
        var metrics = await _dbContext.MetricDefinitionDrafts
            .AsNoTracking()
            .Where(draft => draft.CompanyId == request.CompanyId && draft.AttemptId == request.AttemptId)
            .ToListAsync(cancellationToken);
        var statuses = await _dbContext.StatusDefinitionDrafts
            .AsNoTracking()
            .Where(draft => draft.CompanyId == request.CompanyId && draft.AttemptId == request.AttemptId)
            .ToListAsync(cancellationToken);

        var machineIds = machineDrafts
            .Select(MachineIdOf)
            .Concat(metrics.Select(draft => draft.TargetMachineId))
            .Concat(statuses.Select(draft => draft.TargetMachineId))
            .OfType<Guid>()
            .Distinct()
            .ToList();
        var machineNames = await _dbContext.Machines
            .AsNoTracking()
            .Where(machine => machineIds.Contains(machine.Id) && machine.CompanyId == request.CompanyId)
            .ToDictionaryAsync(machine => machine.Id, machine => machine.Name, cancellationToken);

        var keys = machineDrafts.Select(KeyOf)
            .Concat(metrics.Select(draft => KeyOf(draft.TargetMachineId, draft.TargetMachineDraftId)))
            .Concat(statuses.Select(draft => KeyOf(draft.TargetMachineId, draft.TargetMachineDraftId)))
            .Distinct();
        var groups = keys
            .Select(key => ToGroup(key, machineDrafts, metrics, statuses, machineNames))
            .ToList();

        return new AttemptDraftsResponse { Groups = groups };
    }

    private static Guid? MachineIdOf(MachineDraft draft) => draft.TargetMachineId ?? draft.AppliedMachineId;

    private static Guid KeyOf(MachineDraft draft) => MachineIdOf(draft) ?? draft.Id;

    private static Guid KeyOf(Guid? targetMachineId, Guid? targetMachineDraftId) =>
        targetMachineId ?? targetMachineDraftId!.Value;

    private static AttemptDraftGroup ToGroup(
        Guid key,
        List<MachineDraft> machineDrafts,
        List<MetricDefinitionDraft> metrics,
        List<StatusDefinitionDraft> statuses,
        Dictionary<Guid, string> machineNames
    )
    {
        var machineDraft = machineDrafts.FirstOrDefault(draft => KeyOf(draft) == key);
        var isPendingCreate = machineDraft is not null && MachineIdOf(machineDraft) is null;

        return new AttemptDraftGroup
        {
            MachineId = isPendingCreate ? null : key,
            MachineName = isPendingCreate ? null : machineNames.GetValueOrDefault(key),
            MachineDraft = machineDraft is null ? null : MachineDraftDetail.From(machineDraft),
            Metrics =
            [
                .. metrics.Where(draft => KeyOf(draft.TargetMachineId, draft.TargetMachineDraftId) == key)
                    .Select(MetricDraftDetail.From),
            ],
            Statuses =
            [
                .. statuses.Where(draft => KeyOf(draft.TargetMachineId, draft.TargetMachineDraftId) == key)
                    .Select(StatusDraftDetail.From),
            ],
        };
    }
}
