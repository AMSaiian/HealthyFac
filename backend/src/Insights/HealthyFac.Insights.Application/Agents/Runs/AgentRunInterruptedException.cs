namespace HealthyFac.Insights.Application.Agents.Runs;

/// <summary>
/// Reports that the sweep interrupted a run while its runner still worked. It is no failure: the run is already
/// <c>interrupted</c>, so the runner stops and commits nothing.
/// </summary>
public sealed class AgentRunInterruptedException : Exception
{
    private const string MessageFormat = "The agent run {0} was interrupted.";

    public AgentRunInterruptedException(Guid runId)
        : base(string.Format(MessageFormat, runId))
    {
    }
}
