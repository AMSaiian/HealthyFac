using HealthyFac.Insights.Domain.Agents.Runs;

namespace HealthyFac.Insights.Application.Agents.Runs;

/// <summary>
/// Renews the lease of a running run, so the lease sweep never interrupts a run that still works. A runner renews the
/// lease after each model call and each tool call.
/// </summary>
public interface IAgentRunLease
{
    /// <summary>
    /// Sets the lease of the run to the lease time from now, in one short update that applies only while the run is
    /// <c>running</c>. The tracked run gets the new lease and the new row version, so its later save does not conflict.
    /// </summary>
    /// <param name="run">The tracked run of the caller.</param>
    /// <param name="cancellationToken">The token that cancels the update.</param>
    /// <returns>A task that completes when the lease is renewed.</returns>
    /// <exception cref="AgentRunInterruptedException">
    /// Thrown when the run is no longer <c>running</c>: the sweep interrupted it. The runner must stop and commit
    /// nothing.
    /// </exception>
    Task RenewAsync(AgentRun run, CancellationToken cancellationToken);
}
