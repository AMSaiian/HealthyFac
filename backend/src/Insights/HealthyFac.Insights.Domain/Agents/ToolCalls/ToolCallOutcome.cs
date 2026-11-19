using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Agents.ToolCalls;

/// <summary>
/// Defines how a tool call of an agent ended. The camelCase values reach the database, so never rename one in place.
/// </summary>
public sealed class ToolCallOutcome : Enumeration<ToolCallOutcome>
{
    public static readonly ToolCallOutcome Succeeded = new(value: "succeeded");
    public static readonly ToolCallOutcome Rejected = new(value: "rejected");
    public static readonly ToolCallOutcome Failed = new(value: "failed");

    private ToolCallOutcome(string value)
        : base(value)
    {
    }
}
