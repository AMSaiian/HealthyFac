using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Agents.Actions;

/// <summary>
/// Defines the states of a buffered agent action. The camelCase values reach the database, so never rename one in
/// place.
/// </summary>
public sealed class AgentActionState : Enumeration<AgentActionState>
{
    public static readonly AgentActionState Buffered = new(value: "buffered");
    public static readonly AgentActionState Committed = new(value: "committed");
    public static readonly AgentActionState Discarded = new(value: "discarded");

    private AgentActionState(string value)
        : base(value)
    {
    }
}
