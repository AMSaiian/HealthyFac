using HealthyFac.Insights.Application.Agents.Tools.Writes;
using HealthyFac.Insights.Application.Realtime;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.Realtime;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Agents.Runs;

/// <summary>
/// Ends a run. A completed run commits its buffered actions, and a failed run discards them. The commit changes the
/// signals, the case, the actions, and the run, and it adds the proposal messages to the outbox, all in one save, so
/// no half of a run is ever stored.
/// </summary>
/// <remarks>
/// A <c>proposeWorkOrder</c> action adds <see cref="WorkOrderDraftProposed"/> (the draft ID is the action ID) and sets
/// its signals <c>drafted</c>. A <c>linkWorkOrder</c> action sets its signals <c>linked</c>. A <c>closeCase</c> action
/// closes the case. The actions apply in the order of their IDs, which are time ordered. A run that is no longer
/// <c>running</c> commits nothing, so a second commit stores no second message.
/// </remarks>
/// <param name="dbContext">Loads the actions and saves the changes.</param>
/// <param name="outbox">Stores the proposal messages with the changes.</param>
/// <param name="timeProvider">Provides the end time of the run.</param>
/// <param name="queue">Queues the triage run after a case change.</param>
/// <param name="notifier">Adds the case hint after each save that ends a run. The caller pushes it.</param>
public sealed class RunCommit(
    IInsightsDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider,
    AgentRunQueue queue,
    IRealtimeNotifier notifier
)
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly AgentRunQueue _queue = queue;
    private readonly IRealtimeNotifier _notifier = notifier;

    /// <summary>
    /// Commits the buffered actions of the run and completes it.
    /// </summary>
    /// <param name="run">The tracked, running run.</param>
    /// <param name="maintenanceCase">The tracked case of the run, with its signals.</param>
    /// <param name="verdict">The verdict of the run, or <see langword="null"/> for a role without one.</param>
    /// <param name="verdictReason">The reason of the verdict, or <see langword="null"/>.</param>
    /// <param name="inputTokens">The input tokens of the run.</param>
    /// <param name="outputTokens">The output tokens of the run.</param>
    /// <param name="cancellationToken">The token that cancels the save.</param>
    /// <returns>A task that completes when the commit is saved.</returns>
    public async Task CommitAsync(
        AgentRun run,
        MaintenanceCase maintenanceCase,
        AgentRunVerdict? verdict,
        string? verdictReason,
        int inputTokens,
        int outputTokens,
        CancellationToken cancellationToken
    )
    {
        if (run.State != AgentRunState.Running)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var actions = await LoadBufferedAsync(run.Id, cancellationToken);

        foreach (var action in actions)
        {
            Apply(action, run, maintenanceCase, now);
            action.Commit();
        }

        run.Complete(now, verdict, verdictReason, inputTokens, outputTokens);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _notifier.AddCasesHint(maintenanceCase.CompanyId, maintenanceCase.Id);
    }

    /// <summary>
    /// Discards the buffered actions of the run and ends it <c>failed</c>. The method loads the run again, so it also
    /// works after the change tracker was cleared.
    /// </summary>
    /// <param name="runId">The run to end.</param>
    /// <param name="errorCode">Why the run failed.</param>
    /// <param name="cancellationToken">The token that cancels the save.</param>
    /// <returns>A task that completes when the change is saved.</returns>
    public async Task FailAsync(Guid runId, AgentRunErrorCode errorCode, CancellationToken cancellationToken)
    {
        var run = await DiscardAndFailAsync(runId, errorCode, cancellationToken);

        if (run is null)
        {
            return;
        }

        var companyId = await FindCompanyIdAsync(run, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        AddCasesHint(companyId, run);
    }

    /// <summary>
    /// Discards the buffered actions of the run and ends it <c>failed</c> with the last text of the model and the
    /// tokens of the run.
    /// </summary>
    /// <param name="runId">The run to end.</param>
    /// <param name="errorCode">Why the run failed.</param>
    /// <param name="verdictReason">The last text of the model, or <see langword="null"/>.</param>
    /// <param name="inputTokens">The input tokens of the run.</param>
    /// <param name="outputTokens">The output tokens of the run.</param>
    /// <param name="cancellationToken">The token that cancels the save.</param>
    /// <returns>A task that completes when the change is saved.</returns>
    public async Task FailWithTextAsync(
        Guid runId,
        AgentRunErrorCode errorCode,
        string? verdictReason,
        int inputTokens,
        int outputTokens,
        CancellationToken cancellationToken
    )
    {
        var run = await DiscardAndFailAsync(
            runId, errorCode, cancellationToken, verdictReason, inputTokens, outputTokens);

        if (run is null)
        {
            return;
        }

        var companyId = await FindCompanyIdAsync(run, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        AddCasesHint(companyId, run);
    }

    /// <summary>
    /// Ends a run whose case changed during the run: it discards the actions, ends the run <c>failed</c> with
    /// <see cref="AgentRunErrorCode.CaseChanged"/>, and in the same save queues a triage run when the reloaded case
    /// still has an uncovered open signal and no queued run.
    /// </summary>
    /// <param name="runId">The run to end.</param>
    /// <param name="cancellationToken">The token that cancels the save.</param>
    /// <returns>A task that completes when the change is saved.</returns>
    public async Task FailCaseChangedAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await DiscardAndFailAsync(runId, AgentRunErrorCode.CaseChanged, cancellationToken);

        if (run?.CaseId is not { } caseId)
        {
            return;
        }

        var maintenanceCase = await _dbContext.MaintenanceCases
            .Include(candidate => candidate.Signals)
            .SingleAsync(candidate => candidate.Id == caseId, cancellationToken);

        var isCaseOpen = maintenanceCase.State == CaseState.Open;
        var hasUncoveredSignal = maintenanceCase.Signals.Any(signal => signal.IsOpen && signal.Coverage.IsUncovered);

        if (hasUncoveredSignal && isCaseOpen)
        {
            await _queue.QueueTriageAsync(caseId, run.FinishedAt!.Value, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _notifier.AddCasesHint(maintenanceCase.CompanyId, caseId);
    }

    private async Task<Guid?> FindCompanyIdAsync(AgentRun run, CancellationToken cancellationToken) =>
        run.CaseId is { } caseId
            ? await _dbContext.MaintenanceCases
                .Where(candidate => candidate.Id == caseId)
                .Select(candidate => (Guid?)candidate.CompanyId)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

    private void AddCasesHint(Guid? companyId, AgentRun run)
    {
        if (companyId is { } company && run.CaseId is { } caseId)
        {
            _notifier.AddCasesHint(company, caseId);
        }
    }

    private async Task<AgentRun?> DiscardAndFailAsync(
        Guid runId,
        AgentRunErrorCode errorCode,
        CancellationToken cancellationToken,
        string? verdictReason = null,
        int inputTokens = 0,
        int outputTokens = 0
    )
    {
        var run = await _dbContext.AgentRuns.SingleOrDefaultAsync(
            candidate => candidate.Id == runId && candidate.State == AgentRunState.Running,
            cancellationToken);

        if (run is null)
        {
            return null;
        }

        var actions = await LoadBufferedAsync(runId, cancellationToken);
        actions.ForEach(action => action.Discard());
        run.Fail(_timeProvider.GetUtcNow().UtcDateTime, errorCode, inputTokens, outputTokens, verdictReason);

        return run;
    }

    private async Task<List<AgentAction>> LoadBufferedAsync(Guid runId, CancellationToken cancellationToken) =>
        await _dbContext.AgentActions
            .Where(action => action.RunId == runId && action.State == AgentActionState.Buffered)
            .OrderBy(action => action.Id)
            .ToListAsync(cancellationToken);

    private void Apply(AgentAction action, AgentRun run, MaintenanceCase maintenanceCase, DateTime now)
    {
        if (action.Kind == AgentActionKind.ProposeWorkOrder)
        {
            ApplyProposal(action, run, maintenanceCase);
        }
        else if (action.Kind == AgentActionKind.LinkWorkOrder)
        {
            var payload = ActionBuffering.Deserialize<LinkWorkOrderPayload>(action);
            SignalsOf(maintenanceCase, payload.SignalIds).ForEach(signal => signal.MarkLinked(payload.WorkOrderId));
        }
        else if (action.Kind == AgentActionKind.CloseCase)
        {
            var payload = ActionBuffering.Deserialize<CloseCasePayload>(action);

            if (maintenanceCase.State == CaseState.Open)
            {
                maintenanceCase.Close(payload.Reason, now);
            }
        }
    }

    private void ApplyProposal(AgentAction action, AgentRun run, MaintenanceCase maintenanceCase)
    {
        var payload = ActionBuffering.Deserialize<ProposeWorkOrderPayload>(action);
        SignalsOf(maintenanceCase, payload.SignalIds).ForEach(signal => signal.MarkDrafted(action.Id));

        _outbox.Add(
            new WorkOrderDraftProposed
            {
                DraftId = action.Id,
                CompanyId = maintenanceCase.CompanyId,
                MachineId = maintenanceCase.MachineId,
                CaseId = maintenanceCase.Id,
                RunId = run.Id,
                Title = payload.Title,
                Description = payload.Description,
                Priority = payload.Priority,
                Category = payload.Category,
                EstimateMinutes = payload.EstimateMinutes,
            });
    }

    private static List<MachineSignal> SignalsOf(MaintenanceCase maintenanceCase, IReadOnlyList<Guid> signalIds) =>
        [.. maintenanceCase.Signals.Where(signal => signalIds.Contains(signal.Id))];
}
