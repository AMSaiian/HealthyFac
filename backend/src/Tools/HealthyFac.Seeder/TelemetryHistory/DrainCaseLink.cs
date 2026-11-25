using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Tools.Writes;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>
/// Seeds the result of the planner agent for a drain case without an AI call: 1 completed <c>planner</c> run with 1
/// committed <c>linkWorkOrder</c> action. The signals of the case that are open get the coverage <c>linked</c>, as
/// <c>RunCommit</c> does it.
/// </summary>
internal static class DrainCaseLink
{
    /// <summary>Links the open signals of the open case of the machine to a work order, in one save.</summary>
    /// <remarks>
    /// The save uses the token (no durable step before it), so a cancelled run stores no run and no action.
    /// </remarks>
    /// <param name="insights">The Insights host.</param>
    /// <param name="item">The item of the link event, for the error.</param>
    /// <param name="drain">The drain whose case the work order handles.</param>
    /// <param name="companyId">The company.</param>
    /// <param name="machineId">The machine of the drain.</param>
    /// <param name="workOrderId">The work order that covers the signals.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the link is stored.</returns>
    /// <exception cref="SeedFileException">The machine has no open case at the link moment.</exception>
    public static async Task RunAsync(
        ServiceHosts.ServiceHost insights,
        string item,
        DrainSeed drain,
        Guid companyId,
        Guid machineId,
        Guid workOrderId,
        CancellationToken cancellationToken
    )
    {
        using var scope =
            insights.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Insights, companyId));
        var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var openCase = await dbContext.MaintenanceCases
            .Where(candidate => candidate.MachineId == machineId && candidate.State == CaseState.Open)
            .SingleOrDefaultAsync(cancellationToken);
        if (openCase is null)
        {
            throw new SeedFileException(TelemetryHistoryErrors.LinkNeedsOpenCase(item, drain.Machine));
        }

        var signals = await dbContext.MachineSignals
            .Where(signal => signal.CaseId == openCase.Id && signal.State == SignalState.Open)
            .ToListAsync(cancellationToken);
        var run = new AgentRun(AgentRole.Planner, openCase.Id, ingestAttemptId: null, now);
        run.Claim(now, now);
        run.Complete(now, verdict: null, verdictReason: null, inputTokens: 0, outputTokens: 0);
        var action = new AgentAction(
            run.Id,
            $"seed-{drain.Key}",
            AgentActionKind.LinkWorkOrder,
            ActionBuffering.Serialize(
                new LinkWorkOrderPayload
                    { SignalIds = signals.Select(signal => signal.Id).ToList(), WorkOrderId = workOrderId }));
        action.Commit();
        signals.ForEach(signal => signal.MarkLinked(workOrderId));
        dbContext.AgentRuns.Add(run);
        dbContext.AgentActions.Add(action);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
