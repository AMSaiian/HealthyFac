using FluentValidation;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Cases;

public record GetCaseRequest : IRequest<CaseDetailResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }
}

public sealed class GetCaseRequestValidator : AbstractValidator<GetCaseRequest>
{
    public GetCaseRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(CaseErrors.CompanyIdRequired);
        RuleFor(request => request.Id).NotEmpty().WithMessage(CaseErrors.CaseIdRequired);
    }
}

public sealed class GetCasePreHandlerCheck(ICurrentUserProvider currentUser) : IPreHandlerCheck<GetCaseRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetCaseRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads the header of one case with its outcome. The signals and the runs have their own paged reads
/// (<see cref="SearchCaseSignalsRequest"/>, <see cref="SearchCaseRunsRequest"/>). A case of another company is "not
/// found".
/// </summary>
/// <param name="dbContext">Reads the case, the runs, and the committed actions.</param>
public sealed class GetCaseRequestHandler(IInsightsDbContext dbContext)
    : IRequestHandler<GetCaseRequest, CaseDetailResponse>
{
    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task<CaseDetailResponse> HandleAsync(
        GetCaseRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var maintenanceCase = await _dbContext.MaintenanceCases
                .AsNoTracking()
                .Include(candidate => candidate.Signals)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
            ?? throw new NotFoundException(CaseErrors.CaseNotFound);
        var runs = _dbContext.AgentRuns.AsNoTracking().Where(run => run.CaseId == request.Id);
        var lastRun = await runs.OrderByDescending(run => run.Id).FirstOrDefaultAsync(cancellationToken);
        var decisionRuns = runs.Where(run => run.Role == AgentRole.Triage || run.Role == AgentRole.Planner);
        var decisionRun = await decisionRuns
            .Where(run => run.State == AgentRunState.Completed || run.State == AgentRunState.Failed)
            .OrderByDescending(run => run.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var reason = await decisionRuns
            .Where(run => run.VerdictReason != null)
            .OrderByDescending(run => run.Id)
            .Select(run => run.VerdictReason)
            .FirstOrDefaultAsync(cancellationToken);
        var latestAction = await _dbContext.AgentActions
            .AsNoTracking()
            .Where(action => action.State == AgentActionState.Committed
                && runs.Any(run => run.Id == action.RunId))
            .OrderByDescending(action => action.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var isAiUnavailable = lastRun is not null && IsAiUnavailable(lastRun);

        return new CaseDetailResponse
        {
            Id = maintenanceCase.Id,
            MachineId = maintenanceCase.MachineId,
            State = maintenanceCase.State,
            HighestOpenSeverity = HighestOpenSeverity(maintenanceCase),
            SignalCount = maintenanceCase.Signals.Count,
            LastRunState = lastRun?.State,
            IsAiUnavailable = isAiUnavailable,
            OpenedAt = maintenanceCase.OpenedAt,
            ClosedAt = maintenanceCase.ClosedAt,
            CloseReason = maintenanceCase.CloseReason,
            Outcome = CaseOutcomes.Resolve(latestAction, decisionRun, isAiUnavailable, reason),
        };
    }

    private static SignalSeverity? HighestOpenSeverity(MaintenanceCase maintenanceCase)
    {
        var openSignals = maintenanceCase.Signals.Where(signal => signal.IsOpen).ToList();

        if (openSignals.Count == 0)
        {
            return null;
        }

        return openSignals.Any(signal => signal.Severity == SignalSeverity.Alarm)
            ? SignalSeverity.Alarm
            : SignalSeverity.Warning;
    }

    private static bool IsAiUnavailable(AgentRun run) =>
        run.State == AgentRunState.Interrupted
        || (run.State == AgentRunState.Failed
            && (run.ErrorCode == AgentRunErrorCode.ProviderUnavailable
                || run.ErrorCode == AgentRunErrorCode.UnexpectedError));
}
