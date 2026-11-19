using FluentValidation;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.Application.Agents.Runs;

public record RunAgentRequest : IRequest
{
    public required Guid RunId { get; init; }

    public required Guid CompanyId { get; init; }
}

public class RunAgentRequestValidator : AbstractValidator<RunAgentRequest>
{
    public RunAgentRequestValidator()
    {
        RuleFor(x => x.RunId).NotEmpty().WithMessage(AgentMessages.RunIdRequired);
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(AgentMessages.CompanyIdRequired);
    }
}

/// <summary>
/// Checks that the actor works inside the company of the run. The worker restores the advisor actor of the company
/// of the case, so a run of another company is out of scope.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class RunAgentPreHandlerCheck(ICurrentUserProvider currentUser) : IPreHandlerCheck<RunAgentRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(RunAgentRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Runs a claimed agent run. A case that is not <c>open</c>, or that has no uncovered open signal, needs no model
/// call: the run ends <c>completed</c> at once. Otherwise the handler gives the run to the runner of its role. A role
/// without a runner ends the run <c>failed</c> with the code <c>unsupportedRole</c>.
/// </summary>
/// <remarks>
/// An unexpected error of a runner (a bug, a failed save) must not leave the run <c>running</c>, because the lease
/// sweep would then replace the run again and again, with a new model call each time. The handler logs the error,
/// drops every tracked change, loads the run again, discards its buffered actions, ends it <c>failed</c> with the
/// code <c>unexpectedError</c>, and saves. A cancellation is no failure and propagates. A run that the sweep already
/// interrupted (<see cref="AgentRunInterruptedException"/>) is no failure either: the handler drops the tracked
/// changes, so nothing of the run is committed.
/// </remarks>
/// <param name="dbContext">Loads the run and its case.</param>
/// <param name="runners">The runners of the roles.</param>
/// <param name="timeProvider">Provides the end time of a run that needs no model call.</param>
/// <param name="commit">Ends a run that failed with an unexpected error.</param>
/// <param name="logger">Logs an unexpected error of a run.</param>
public sealed class RunAgentRequestHandler(
    IInsightsDbContext dbContext,
    IEnumerable<IAgentRunner> runners,
    TimeProvider timeProvider,
    RunCommit commit,
    ILogger<RunAgentRequestHandler> logger
) : RequestHandler<RunAgentRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly IReadOnlyList<IAgentRunner> _runners = runners.ToList();
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly RunCommit _commit = commit;
    private readonly ILogger<RunAgentRequestHandler> _logger = logger;

    protected override async Task HandleInternalAsync(RunAgentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await RunAsync(request, cancellationToken);
        }
        catch (AgentRunInterruptedException)
        {
            _dbContext.ChangeTracker.Clear();
        }
        catch (DbUpdateConcurrencyException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                message: "The case of the agent run {RunId} changed during the run.",
                request.RunId);
            _dbContext.ChangeTracker.Clear();
            await _commit.FailCaseChangedAsync(request.RunId, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                message: "The agent run {RunId} failed with an unexpected error.",
                request.RunId);
            await FailRunAsync(request.RunId, cancellationToken);
        }
    }

    private static bool NeedsRun(MaintenanceCase maintenanceCase) =>
        maintenanceCase.State == CaseState.Open
        && maintenanceCase.Signals.Any(signal => signal.IsOpen && signal.Coverage.IsUncovered);

    private async Task FailRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        _dbContext.ChangeTracker.Clear();
        await _commit.FailAsync(runId, AgentRunErrorCode.UnexpectedError, cancellationToken);
    }

    private async Task RunAsync(RunAgentRequest request, CancellationToken cancellationToken)
    {
        var run = await _dbContext.AgentRuns.SingleOrDefaultAsync(
            candidate => candidate.Id == request.RunId && candidate.State == AgentRunState.Running,
            cancellationToken);

        if (run is null)
        {
            return;
        }

        var maintenanceCase = run.CaseId is { } caseId
            ? await _dbContext.MaintenanceCases
                .Include(candidate => candidate.Signals)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == caseId && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
            : null;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (maintenanceCase is null || maintenanceCase.State != CaseState.Open)
        {
            run.Skip(now, AgentRunSkipReason.CaseNotOpen);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return;
        }

        if (!NeedsRun(maintenanceCase))
        {
            run.Skip(now, AgentRunSkipReason.NothingToReview);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return;
        }

        var runner = _runners.SingleOrDefault(candidate => candidate.Role == run.Role);

        if (runner is null)
        {
            run.Fail(now, AgentRunErrorCode.UnexpectedError);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return;
        }

        await runner.RunAsync(run, maintenanceCase, cancellationToken);
    }
}
