using System.Reflection;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Shared.Application.DownstreamScopes;

namespace HealthyFac.Insights.ArchitectureTests.Conventions.Data;

/// <summary>
/// Finds the violations of the tool rules in a set of types: a tool declares its downstream scopes or is local, a
/// tool name belongs to one tool of the whole app, and a chat tool declares a permission and an activity and no
/// downstream scopes. The tools that read no protected data (<c>report_no_answer</c> and <c>get_current_time</c>) need
/// no permission, and only <c>report_no_answer</c> needs no activity.
/// </summary>
public static class AgentToolConventionData
{
    private static readonly string[] ToolsWithoutPermission = ["report_no_answer", "get_current_time"];

    private static readonly string[] ToolsWithoutActivity = ["report_no_answer"];

    public static IReadOnlyList<string> FindViolations(IEnumerable<Type> types)
    {
        var tools = types
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<AgentToolAttribute>()))
            .Where(tool => tool.Attribute is not null)
            .ToList();

        var violations = new List<string>();

        violations.AddRange(
            tools
                .Where(tool => tool.Attribute!.Role != AgentRole.Chat.Value
                    && tool.Type.GetCustomAttribute<DownstreamScopesAttribute>() is null
                    && tool.Type.GetCustomAttribute<LocalToolAttribute>() is null)
                .Select(tool => $"{tool.Type.FullName} has neither [DownstreamScopes] nor [LocalTool]."));

        violations.AddRange(
            tools
                .GroupBy(tool => tool.Attribute!.Name)
                .Where(group => group.Count() > 1)
                .Select(group => $"The tool name '{group.Key}' belongs to {group.Count()} tools."));

        violations.AddRange(FindChatViolations(tools.Select(tool => (tool.Type, tool.Attribute!))));

        return violations;
    }

    private static IEnumerable<string> FindChatViolations(IEnumerable<(Type Type, AgentToolAttribute Attribute)> tools)
    {
        var chatTools = tools.Where(tool => tool.Attribute.Role == AgentRole.Chat.Value).ToList();

        return chatTools
            .Where(tool =>
                tool.Attribute.Permissions.Length == 0 && !ToolsWithoutPermission.Contains(tool.Attribute.Name))
            .Select(tool => $"The chat tool {tool.Type.FullName} declares no permission.")
            .Concat(
                chatTools
                    .Where(tool => !ToolsWithoutActivity.Contains(tool.Attribute.Name)
                        && !ChatActivity.TryFromValue(tool.Attribute.Activity, out _))
                    .Select(tool => $"The chat tool {tool.Type.FullName} declares no valid activity."))
            .Concat(
                chatTools
                    .Where(tool => tool.Type.GetCustomAttribute<DownstreamScopesAttribute>() is not null)
                    .Select(tool => $"The chat tool {tool.Type.FullName} has [DownstreamScopes]."));
    }
}
