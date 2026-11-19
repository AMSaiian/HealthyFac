using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Agents.Runs;

/// <summary>
/// Defines the verdicts that a run stores. The triage gives <c>dismiss</c>, <c>watch</c>, or <c>escalate</c>. The
/// screener gives <c>safe</c> or <c>unsafe</c>. A planner run and an extractor run have no verdict. The camelCase
/// values reach the database, so never rename one in place.
/// </summary>
public sealed class AgentRunVerdict : Enumeration<AgentRunVerdict>
{
    public static readonly AgentRunVerdict Dismiss = new(value: "dismiss");
    public static readonly AgentRunVerdict Watch = new(value: "watch");
    public static readonly AgentRunVerdict Escalate = new(value: "escalate");
    public static readonly AgentRunVerdict Safe = new(value: "safe");
    public static readonly AgentRunVerdict Unsafe = new(value: "unsafe");

    private AgentRunVerdict(string value)
        : base(value)
    {
    }
}
