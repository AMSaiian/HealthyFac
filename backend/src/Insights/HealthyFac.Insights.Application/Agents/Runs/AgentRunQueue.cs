using HealthyFac.Insights.Domain.Agents.Runs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Agents.Runs;

/// <summary>
/// Queues the runs of the agents. A run is a row in the database that a worker claims at its due time.
/// </summary>
/// <param name="dbContext">Reads the queued runs and adds a run.</param>
/// <param name="options">Holds the debounce of a triage.</param>
public sealed class AgentRunQueue(IInsightsDbContext dbContext, IOptions<AgentOptions> options)
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly AgentOptions _options = options.Value;

    /// <summary>
    /// Adds a triage run for a case, unless the case already has a queued run. The run is due after the debounce, so
    /// the signals that arrive in the meantime join the same run. The caller saves the change.
    /// </summary>
    /// <param name="caseId">The case that the triage looks at.</param>
    /// <param name="now">The current time (UTC).</param>
    /// <returns>A task that completes when the run is added or skipped.</returns>
    public async Task QueueTriageAsync(Guid caseId, DateTime now, CancellationToken cancellationToken)
    {
        var hasQueuedRun = await _dbContext.AgentRuns.AnyAsync(
            run => run.CaseId == caseId && run.State == AgentRunState.Queued,
            cancellationToken);

        if (hasQueuedRun)
        {
            return;
        }

        _dbContext.AgentRuns.Add(
            new AgentRun(
                AgentRole.Triage,
                caseId,
                ingestAttemptId: null,
                now.AddSeconds(_options.RunDebounceSeconds)));
    }
}
