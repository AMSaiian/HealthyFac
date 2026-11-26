using System.Reflection;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Domain.Agents.Runs;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.Application.Agents.Tools;

/// <summary>
/// Builds the functions of the tools of one role. A tool is a class with <see cref="AgentToolAttribute"/> that
/// implements <see cref="IAgentToolRequest{TRequest,TArguments,TResponse}"/>. A role gets only its own tools.
/// </summary>
public sealed class ToolRegistry
{
    private const string NotAToolMessage = "{0} has [AgentTool] and does not implement IAgentToolRequest.";

    private readonly IAdvisorScopes _scopes;
    private readonly IReadOnlyList<Type> _toolTypes;
    private readonly ILogger _logger;
    private readonly IAgentJson _json;

    /// <summary>
    /// Creates the registry over the given tool types.
    /// </summary>
    /// <param name="scopes">Creates the scope of a tool call.</param>
    /// <param name="toolTypes">The tool request types. <see cref="FindToolTypes"/> finds them in an assembly.</param>
    /// <param name="logger">Receives the failure of a tool call.</param>
    /// <param name="json">The options that encrypt the IDs of the arguments and the results.</param>
    public ToolRegistry(IAdvisorScopes scopes, IEnumerable<Type> toolTypes, ILogger logger, IAgentJson json)
    {
        _json = json;
        _logger = logger;
        _scopes = scopes;
        _toolTypes = [.. toolTypes];
    }

    /// <summary>
    /// Finds the classes with <see cref="AgentToolAttribute"/> in an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The tool request types.</returns>
    public static IReadOnlyList<Type> FindToolTypes(Assembly assembly) =>
        [.. assembly.GetTypes().Where(type => type.GetCustomAttribute<AgentToolAttribute>() is not null)];

    /// <summary>
    /// Builds one function for each tool of the role.
    /// </summary>
    /// <param name="role">The role of the run.</param>
    /// <param name="context">The values of the run that the tools use.</param>
    /// <returns>The functions of the role.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a type with the attribute does not implement the tool interface.
    /// </exception>
    public IReadOnlyList<AIFunction> For(AgentRole role, ToolContext context) =>
    [
        .. _toolTypes
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<AgentToolAttribute>()!))
            .Where(tool => tool.Attribute.Role == role.Value)
            .Select(tool => Build(tool.Type, tool.Attribute, context)),
    ];

    private AIFunction Build(Type type, AgentToolAttribute attribute, ToolContext context)
    {
        var contract = type.GetInterfaces()
                .SingleOrDefault(candidate => candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == typeof(IAgentToolRequest<,,>))
            ?? throw new InvalidOperationException(string.Format(NotAToolMessage, type.Name));

        var arguments = contract.GetGenericArguments();
        var functionType = typeof(ToolFunction<,,>).MakeGenericType(type, arguments[1], arguments[2]);

        return (AIFunction)Activator.CreateInstance(functionType, attribute, _scopes, context, _logger, _json)!;
    }
}
