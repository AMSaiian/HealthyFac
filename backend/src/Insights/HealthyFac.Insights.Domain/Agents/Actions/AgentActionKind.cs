using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Agents.Actions;

/// <summary>
/// Defines the kinds of write that an agent buffers during a run. The camelCase values reach the database, so never
/// rename one in place.
/// </summary>
public sealed class AgentActionKind : Enumeration<AgentActionKind>
{
    public static readonly AgentActionKind ProposeWorkOrder = new(value: "proposeWorkOrder");
    public static readonly AgentActionKind LinkWorkOrder = new(value: "linkWorkOrder");
    public static readonly AgentActionKind CloseCase = new(value: "closeCase");

    private AgentActionKind(string value)
        : base(value)
    {
    }
}
