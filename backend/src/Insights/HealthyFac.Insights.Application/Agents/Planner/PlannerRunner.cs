using System.Text.Json;
using HealthyFac.Insights.Application.Agents.Prompts;
using HealthyFac.Insights.Application.Agents.Runs;
using HealthyFac.Insights.Application.Agents.Snapshots;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Agents.Tools.Writes;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.Application.Agents.Planner;

/// <summary>
/// Runs the planner of a case: the model reads the snapshot of the case, calls the read tools (open work orders,
/// metric history, signal episodes, manual search) and the write tools (propose a work order, link a work order, close
/// the case), and ends. The write tools only buffer their actions. When the loop ends without an error, the runner
/// commits the actions and completes the run in one save (<see cref="RunCommit"/>). A provider failure (after the
/// retries of each model call, see <see cref="IPlannerModel"/>) discards the actions and ends the run <c>failed</c>
/// with the code <c>providerUnavailable</c>. The lease of the run renews after each model call and each tool call.
/// A planner run that ends without a buffered write action gets one decision turn in the model (see
/// <see cref="IPlannerModel"/>). A run that still has no write action ends <c>failed</c> with the code
/// <c>noDecision</c> and commits nothing. The last text of the model is stored on the run.
/// </summary>
/// <param name="model">Runs the loop of the model and the tools.</param>
/// <param name="prompts">Provides the system prompt.</param>
/// <param name="registry">Builds the planner tools.</param>
/// <param name="toolCallIds">Provides the ID of the running tool call.</param>
/// <param name="lease">Renews the lease of the run.</param>
/// <param name="commit">Commits or discards the actions and ends the run.</param>
/// <param name="dbContext">Stores the tool calls.</param>
/// <param name="json">Gives the JSON options of the case snapshot.</param>
/// <param name="logger">Logs a run that ended without a decision.</param>
public sealed class PlannerRunner(
    IPlannerModel model,
    IAgentPrompts prompts,
    ToolRegistry registry,
    IToolCallIds toolCallIds,
    IAgentRunLease lease,
    RunCommit commit,
    IInsightsDbContext dbContext,
    IAgentJson json,
    ILogger<PlannerRunner> logger
) : IAgentRunner
{
    public const string CaseBlockTag = "case";

    private readonly IPlannerModel _model = model;
    private readonly IAgentPrompts _prompts = prompts;
    private readonly ToolRegistry _registry = registry;
    private readonly IToolCallIds _toolCallIds = toolCallIds;
    private readonly IAgentRunLease _lease = lease;
    private readonly RunCommit _commit = commit;
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly IAgentJson _json = json;
    private readonly ILogger<PlannerRunner> _logger = logger;

    public AgentRole Role => AgentRole.Planner;

    private static readonly HashSet<string> WriteToolNames =
    [
        ProposeWorkOrderToolRequest.ToolName,
        LinkWorkOrderToolRequest.ToolName,
        CloseCaseToolRequest.ToolName,
    ];

    private async Task<bool> HasBufferedActionAsync(Guid runId, CancellationToken cancellationToken) =>
        await _dbContext.AgentActions.AnyAsync(
            action => action.RunId == runId && action.State == AgentActionState.Buffered,
            cancellationToken);

    public async Task RunAsync(AgentRun run, MaintenanceCase maintenanceCase, CancellationToken cancellationToken)
    {
        var prompt = _prompts.For(AgentRole.Planner);
        run.UseModel(_model.ModelName, prompt.Version, prompt.Hash);
        var context = new ToolContext
        {
            CompanyId = maintenanceCase.CompanyId,
            MachineId = maintenanceCase.MachineId,
            RunId = run.Id,
            CaseId = maintenanceCase.Id,
            Recorder = ToolCallRecorder.ForDatabase(_dbContext, run.Id),
            CurrentCallId = _toolCallIds.Current,
            EndLoop = _toolCallIds.EndLoop,
            RenewLease = token => _lease.RenewAsync(run, token),
        };
        var tools = _registry.For(AgentRole.Planner, context);
        var userMessage = AgentDataBlocks.Wrap(
            CaseBlockTag,
            JsonSerializer.Serialize(CaseSnapshot.From(maintenanceCase), _json.Snapshots));

        PlannerModelResult result;

        try
        {
            result = await _model.RunAsync(
                prompt.Text,
                userMessage,
                tools,
                [.. tools.Where(tool => WriteToolNames.Contains(tool.Name))],
                async token => context.ReadFailures.Outage is null && !await HasBufferedActionAsync(run.Id, token),
                cancellationToken);
        }
        catch (ProviderUnavailableException)
        {
            await _commit.FailAsync(run.Id, AgentRunErrorCode.ProviderUnavailable, cancellationToken);

            return;
        }

        if (context.ReadFailures.Outage is not null)
        {
            await _commit.FailAsync(run.Id, AgentRunErrorCode.ProviderUnavailable, cancellationToken);

            return;
        }

        var finalText = result.FinalText is { Length: > AgentRun.VerdictReasonMaxLength }
            ? result.FinalText[..AgentRun.VerdictReasonMaxLength]
            : result.FinalText;

        if (!await HasBufferedActionAsync(run.Id, cancellationToken))
        {
            _logger.LogWarning(message: "The planner run {RunId} ended without a write action.", run.Id);
            await _commit.FailWithTextAsync(
                run.Id,
                AgentRunErrorCode.NoDecision,
                finalText,
                result.InputTokens,
                result.OutputTokens,
                cancellationToken);

            return;
        }

        await _lease.RenewAsync(run, cancellationToken);
        await _commit.CommitAsync(
            run,
            maintenanceCase,
            verdict: null,
            finalText,
            result.InputTokens,
            result.OutputTokens,
            cancellationToken);
    }
}
