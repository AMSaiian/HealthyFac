using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Agents.Runs;

/// <summary>
/// Defines the roles of an agent run. The camelCase values reach the database, so never rename one in place.
/// </summary>
public sealed class AgentRole : Enumeration<AgentRole>
{
    public static readonly AgentRole Triage = new(value: "triage");
    public static readonly AgentRole Planner = new(value: "planner");
    public static readonly AgentRole Extractor = new(value: "extractor");
    public static readonly AgentRole Screener = new(value: "screener");
    public static readonly AgentRole Chat = new(value: "chat");

    private AgentRole(string value)
        : base(value)
    {
    }
}
