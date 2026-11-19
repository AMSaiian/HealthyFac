using System.Text.Json;
using HealthyFac.Insights.Application.Agents.Prompts;
using HealthyFac.Insights.Application.Agents.Runs;
using HealthyFac.Insights.Application.Agents.Snapshots;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;

namespace HealthyFac.Insights.Application.Agents.Triage;

/// <summary>
/// Runs the triage of a case: one call of the small model with the snapshot of the case as a data block, then one
/// save with the verdict. <c>dismiss</c> dismisses the uncovered open signals, and it closes the case with the reason
/// <c>triage</c> only when no open signal is <c>drafted</c> or <c>linked</c>. <c>watch</c> sets the uncovered open
/// signals to <c>watched</c>. <c>escalate</c> queues a planner run that is due now. A provider failure ends the run
/// <c>failed</c> with the code <c>providerUnavailable</c> and changes no signal.
/// </summary>
/// <param name="dbContext">Saves the run, the signals, the case, and the planner run.</param>
/// <param name="model">Asks the triage model.</param>
/// <param name="providerCalls">Retries a transient failure of the model.</param>
/// <param name="prompts">Provides the system prompt.</param>
/// <param name="lease">Renews the lease of the run after the model call.</param>
/// <param name="timeProvider">Provides the end time of the run.</param>
public sealed class TriageRunner(
    IInsightsDbContext dbContext,
    ITriageModel model,
    IAgentProviderCalls providerCalls,
    IAgentPrompts prompts,
    IAgentRunLease lease,
    TimeProvider timeProvider,
    IAgentJson json
) : IAgentRunner
{
    public const string CloseReason = "triage";
    public const int ReasonMaxLength = 1000;
    public const string CaseBlockTag = "case";

    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ITriageModel _model = model;
    private readonly IAgentProviderCalls _providerCalls = providerCalls;
    private readonly IAgentPrompts _prompts = prompts;
    private readonly IAgentRunLease _lease = lease;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IAgentJson _json = json;

    public AgentRole Role => AgentRole.Triage;

    public async Task RunAsync(AgentRun run, MaintenanceCase maintenanceCase, CancellationToken cancellationToken)
    {
        var prompt = _prompts.For(AgentRole.Triage);
        run.UseModel(_model.ModelName, prompt.Version, prompt.Hash);
        var userMessage = AgentDataBlocks.Wrap(
            CaseBlockTag,
            JsonSerializer.Serialize(CaseSnapshot.From(maintenanceCase), _json.Snapshots));

        TriageModelAnswer answer;

        try
        {
            answer = await _providerCalls.ExecuteAsync(
                token => _model.AskAsync(prompt.Text, userMessage, token),
                cancellationToken);
        }
        catch (ProviderUnavailableException)
        {
            run.Fail(_timeProvider.GetUtcNow().UtcDateTime, AgentRunErrorCode.ProviderUnavailable);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return;
        }

        await _lease.RenewAsync(run, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var verdict = TriageVerdictKind.FromValue(ToValue(answer.Verdict.Verdict));
        Apply(verdict, maintenanceCase, now);

        var reason = answer.Verdict.Reason.Length > ReasonMaxLength
            ? answer.Verdict.Reason[..ReasonMaxLength]
            : answer.Verdict.Reason;
        run.Complete(now, AgentRunVerdict.FromValue(verdict.Value), reason, answer.InputTokens, answer.OutputTokens);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ToValue(TriageChoice choice) => JsonNamingPolicy.CamelCase.ConvertName(choice.ToString());

    private void Apply(TriageVerdictKind verdict, MaintenanceCase maintenanceCase, DateTime now)
    {
        var uncovered = maintenanceCase.Signals
            .Where(signal => signal.IsOpen && signal.Coverage.IsUncovered)
            .ToList();

        if (verdict == TriageVerdictKind.Dismiss)
        {
            uncovered.ForEach(signal => signal.Dismiss());
            var hasCoveredOpenSignal = maintenanceCase.Signals.Any(signal => signal.IsOpen
                && (signal.Coverage == SignalCoverage.Drafted || signal.Coverage == SignalCoverage.Linked));

            if (!hasCoveredOpenSignal)
            {
                maintenanceCase.Close(CloseReason, now);
            }
        }
        else if (verdict == TriageVerdictKind.Watch)
        {
            uncovered.ForEach(signal => signal.Watch());
        }
        else
        {
            _dbContext.AgentRuns.Add(new AgentRun(AgentRole.Planner, maintenanceCase.Id, ingestAttemptId: null, now));
        }
    }
}
