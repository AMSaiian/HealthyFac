using System.Reflection;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Shared.Application.DownstreamScopes;

namespace HealthyFac.Insights.ArchitectureTests.Conventions.Data;

/// <summary>
/// Finds the violations of the tool rules in a set of types: a tool declares its downstream scopes or is local, and
/// a tool name belongs to one tool of one role.
/// </summary>
public static class AgentToolConventionData
{
    public static IReadOnlyList<string> FindViolations(IEnumerable<Type> types)
    {
        var tools = types
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<AgentToolAttribute>()))
            .Where(tool => tool.Attribute is not null)
            .ToList();
        var violations = new List<string>();

        violations.AddRange(
            tools
                .Where(tool => tool.Type.GetCustomAttribute<DownstreamScopesAttribute>() is null
                    && tool.Type.GetCustomAttribute<LocalToolAttribute>() is null)
                .Select(tool => $"{tool.Type.FullName} has neither [DownstreamScopes] nor [LocalTool]."));
        violations.AddRange(
            tools
                .GroupBy(tool => tool.Attribute!.Name)
                .Where(group => group.Count() > 1)
                .Select(group => $"The tool name '{group.Key}' belongs to {group.Count()} tools."));

        return violations;
    }
}
