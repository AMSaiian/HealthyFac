using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;

namespace HealthyFac.Insights.Application.Agents.Runs;

/// <summary>
/// Runs the model of one agent role for a run of a case. <see cref="RunAgentRequestHandler"/> picks the runner of the
/// role of the run, after it found that the case still needs the run.
/// </summary>
public interface IAgentRunner
{
    /// <summary>The role that the runner serves.</summary>
    AgentRole Role { get; }

    /// <summary>
    /// Runs the role for the run. The runner ends the run (<c>completed</c> or <c>failed</c>) and saves its changes.
    /// </summary>
    /// <param name="run">The claimed run.</param>
    /// <param name="maintenanceCase">The open case of the run, with its signals.</param>
    /// <param name="cancellationToken">The token that cancels the run.</param>
    /// <returns>A task that completes when the run is saved.</returns>
    Task RunAsync(AgentRun run, MaintenanceCase maintenanceCase, CancellationToken cancellationToken);
}
