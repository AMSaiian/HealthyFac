using HealthyFac.Shared.Application.CurrentUser.Actors;

namespace HealthyFac.Insights.Infrastructure.Agents;

/// <summary>
/// Defines the service actors of the agents. The sweep and the claim run as the worker flow. A run and each tool call
/// of a run work as the advisor flow of the company of the run, so every call to another service carries that
/// company.
/// </summary>
public static class AgentServiceActors
{
    public const string WorkerFlow = "agentWorker";
    public const string AdvisorFlow = "advisor";

    /// <summary>The actor of the sweep and the claim: the flow <c>insights:agentWorker</c>.</summary>
    public static ActorContext Worker { get; } = KnownActors.ForServiceFlow(ServiceName.Insights, WorkerFlow);

    /// <summary>
    /// Creates the actor of a run: the flow <c>insights:advisor</c> of one company.
    /// </summary>
    /// <param name="companyId">The company of the run.</param>
    /// <returns>The service actor of the company.</returns>
    public static ActorContext Advisor(Guid companyId) =>
        KnownActors.ForServiceFlow(ServiceName.Insights, AdvisorFlow, companyId);
}
