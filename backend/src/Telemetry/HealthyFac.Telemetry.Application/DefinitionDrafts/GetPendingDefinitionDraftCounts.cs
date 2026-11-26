using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

public record GetPendingDefinitionDraftCountsRequest : IRequest<PendingDefinitionDraftCountsResponse>
{
    public required Guid CompanyId { get; init; }
}

public record PendingDefinitionDraftCountsResponse
{
    public required IReadOnlyList<MachinePendingDraftCount> Counts { get; init; }
}

/// <summary>The number of pending metric and status drafts of one machine.</summary>
public record MachinePendingDraftCount
{
    public required Guid MachineId { get; init; }

    public required int Count { get; init; }
}

public sealed class GetPendingDefinitionDraftCountsRequestValidator
    : AbstractValidator<GetPendingDefinitionDraftCountsRequest>
{
    public GetPendingDefinitionDraftCountsRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(DefinitionDraftErrors.CompanyIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetPendingDefinitionDraftCountsPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetPendingDefinitionDraftCountsRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetPendingDefinitionDraftCountsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Counts the pending metric, status, and phase drafts of each machine of the company, so the Gateway can show the
/// count in the
/// machine list. A draft that waits for its target (<c>awaitingTarget</c>) does not count. A machine with a deleted
/// registration and a machine with no pending draft are left out.
/// </summary>
/// <param name="dbContext">Reads the drafts and the registrations.</param>
public sealed class GetPendingDefinitionDraftCountsRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<GetPendingDefinitionDraftCountsRequest, PendingDefinitionDraftCountsResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<PendingDefinitionDraftCountsResponse> HandleAsync(
        GetPendingDefinitionDraftCountsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var metricMachineIds = _dbContext.MetricDefinitionDrafts
            .Where(draft => draft.CompanyId == request.CompanyId
                && draft.State == DefinitionDraftState.Pending
                && draft.TargetMachineId != null)
            .Select(draft => draft.TargetMachineId!.Value);

        var statusMachineIds = _dbContext.StatusDefinitionDrafts
            .Where(draft => draft.CompanyId == request.CompanyId
                && draft.State == DefinitionDraftState.Pending
                && draft.TargetMachineId != null)
            .Select(draft => draft.TargetMachineId!.Value);

        var phaseMachineIds = _dbContext.OperatingPhaseDrafts
            .Where(draft => draft.CompanyId == request.CompanyId
                && draft.State == DefinitionDraftState.Pending
                && draft.TargetMachineId != null)
            .Select(draft => draft.TargetMachineId!.Value);

        var counts = await metricMachineIds
            .Concat(statusMachineIds)
            .Concat(phaseMachineIds)
            .Where(machineId => _dbContext.MachineRegistrations.Any(registration =>
                registration.Id == machineId && registration.CompanyId == request.CompanyId))
            .GroupBy(machineId => machineId)
            .Select(group => new MachinePendingDraftCount { MachineId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return new PendingDefinitionDraftCountsResponse { Counts = counts };
    }
}
