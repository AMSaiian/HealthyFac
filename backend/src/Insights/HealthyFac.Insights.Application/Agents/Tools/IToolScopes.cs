using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.Application.Agents.Tools;

/// <summary>
/// Creates the DI scope in which a tool call runs. The scope decides the actor of every call to another service.
/// </summary>
public interface IToolScopes
{
    /// <summary>
    /// Creates the scope of one tool call.
    /// </summary>
    /// <param name="companyId">The company of the run.</param>
    /// <returns>The scope. The caller disposes it.</returns>
    IServiceScope Create(Guid companyId);
}
