using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Agents.Actions;

/// <summary>
/// Defines what happened to the work order draft of a committed <c>proposeWorkOrder</c> action. The camelCase values
/// reach the database, so never rename one in place.
/// </summary>
public sealed class AgentActionFollowUp : Enumeration<AgentActionFollowUp>
{
    public static readonly AgentActionFollowUp Applied = new(value: "applied");
    public static readonly AgentActionFollowUp Dismissed = new(value: "dismissed");

    private AgentActionFollowUp(string value)
        : base(value)
    {
    }
}
