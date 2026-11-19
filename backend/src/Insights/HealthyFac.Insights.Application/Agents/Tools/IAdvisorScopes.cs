using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.Application.Agents.Tools;

/// <summary>
/// Creates the DI scope in which a tool runs, with the advisor actor of one company.
/// </summary>
public interface IAdvisorScopes
{
    /// <summary>
    /// Creates a scope whose actor is the advisor service flow of the company.
    /// </summary>
    /// <param name="companyId">The company of the run.</param>
    /// <returns>The scope. The caller disposes it.</returns>
    IServiceScope Create(Guid companyId);
}
