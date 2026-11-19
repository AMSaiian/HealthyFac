using System.Runtime.CompilerServices;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;

namespace HealthyFac.Insights.Domain.Agents;

/// <summary>
/// Defines the exceptions that a run or an action throws when a caller breaks the lifecycle. The Application layer
/// checks the state first, so these exceptions mark a programming error.
/// </summary>
public static class AgentInvariantErrors
{
    public static InvalidOperationException IllegalTransition(
        Guid runId,
        AgentRunState from,
        [CallerMemberName] string action = ""
    ) =>
        new($"Agent run '{runId}' is {from.Value}, so it cannot {action}.");

    public static InvalidOperationException IllegalActionTransition(
        Guid actionId,
        AgentActionState from,
        [CallerMemberName] string action = ""
    ) =>
        new($"Agent action '{actionId}' is {from.Value}, so it cannot {action}.");
}
