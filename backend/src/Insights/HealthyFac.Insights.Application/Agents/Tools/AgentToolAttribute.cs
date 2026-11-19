namespace HealthyFac.Insights.Application.Agents.Tools;

/// <summary>
/// Marks a request as a tool of an agent role. The role is its value text (<c>planner</c>), because an attribute
/// argument must be a constant. <see cref="ToolRegistry"/> checks the value against the roles.
/// </summary>
/// <param name="role">The value of the role that may call the tool.</param>
/// <param name="name">The name that the model sees. One name belongs to one role.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AgentToolAttribute(string role, string name) : Attribute
{
    public string Role { get; } = role;

    public string Name { get; } = name;

    /// <summary>The text that tells the model what the tool does.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Tells that the tool only reads. A failed or rejected call of a read tool means that the run lacks evidence.
    /// </summary>
    public bool IsRead { get; init; }
}
