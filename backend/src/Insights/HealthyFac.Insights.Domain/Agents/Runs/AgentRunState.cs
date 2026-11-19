using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Agents.Runs;

/// <summary>
/// Defines the states of an agent run. The camelCase values reach the database, so never rename one in place.
/// </summary>
public sealed class AgentRunState : Enumeration<AgentRunState>
{
    public static readonly AgentRunState Queued = new(value: "queued");
    public static readonly AgentRunState Running = new(value: "running");
    public static readonly AgentRunState Completed = new(value: "completed");
    public static readonly AgentRunState Failed = new(value: "failed");
    public static readonly AgentRunState Interrupted = new(value: "interrupted");
    public static readonly AgentRunState Skipped = new(value: "skipped");

    private AgentRunState(string value)
        : base(value)
    {
    }
}
