using FluentValidation;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Drafts;

public record GetAttemptDraftsRequest : IRequest<AttemptDraftsResponse>
{
    public required Guid CompanyId { get; init; }

    public Guid? AttemptId { get; init; }

    public Guid? ChatId { get; init; }
}

public record AttemptDraftsResponse
{
    public required IReadOnlyList<AttemptDraftGroup> Groups { get; init; }
}

/// <summary>
/// The machine draft of one attempt for one machine: an existing machine (the target, or the machine that a create
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
}

public class GetAttemptDraftsRequestValidator : AbstractValidator<GetAttemptDraftsRequest>
{
    public GetAttemptDraftsRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(DraftErrors.CompanyIdRequired);
        RuleFor(x => x.AttemptId)
            .NotEmpty()
            .WithMessage(DraftErrors.AttemptIdRequired)
            .When(x => x.AttemptId is not null);
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage(DraftErrors.ChatIdRequired)
            .When(x => x.ChatId is not null);
        RuleFor(x => x)
            .Must(x => x.AttemptId is null != x.ChatId is null)
            .WithMessage(DraftErrors.GetScopeRequired);
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
/// Reads every machine draft of one ingest attempt or one chat (all states). Set exactly one of the two IDs and groups
/// them by machine. The group key is the
/// machine (the target, or the machine that a create draft made) or, for a pending create draft, the draft itself.
/// The metric and status drafts live in Telemetry. An attempt or a chat that Core does not know gives an empty list,
/// because Core
/// owns neither.
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
            .Include(draft => draft.Proposal)
            .Where(draft => draft.CompanyId == request.CompanyId
                && (request.AttemptId == null || draft.Proposal.AttemptId == request.AttemptId)
                && (request.ChatId == null || draft.Proposal.ChatId == request.ChatId))
            .ToListAsync(cancellationToken);

        var machineIds = machineDrafts
            .Select(MachineIdOf)
            .OfType<Guid>()
            .Distinct()
            .ToList();

        var machineNames = await _dbContext.Machines
            .AsNoTracking()
            .Where(machine => machineIds.Contains(machine.Id) && machine.CompanyId == request.CompanyId)
            .ToDictionaryAsync(machine => machine.Id, machine => machine.Name, cancellationToken);

        var groups = machineDrafts
            .Select(KeyOf)
            .Distinct()
            .Select(key => ToGroup(key, machineDrafts, machineNames))
            .ToList();

        return new AttemptDraftsResponse { Groups = groups };
    }

    private static Guid? MachineIdOf(MachineDraft draft) => draft.TargetMachineId ?? draft.AppliedMachineId;

    private static Guid KeyOf(MachineDraft draft) => MachineIdOf(draft) ?? draft.Id;

    private static AttemptDraftGroup ToGroup(
        Guid key,
        List<MachineDraft> machineDrafts,
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
        };
    }
}
