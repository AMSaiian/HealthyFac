using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.Infrastructure.Agents.Tools;

/// <summary>
/// Creates the scope of a tool call with the advisor actor of the company, so every call to another service carries
/// that company.
/// </summary>
/// <param name="scopeFactory">Creates the scope.</param>
public sealed class AdvisorScopes(IServiceScopeFactory scopeFactory) : IAdvisorScopes
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    public IServiceScope Create(Guid companyId) =>
        _scopeFactory.CreateActorScope(AgentServiceActors.Advisor(companyId));
}
