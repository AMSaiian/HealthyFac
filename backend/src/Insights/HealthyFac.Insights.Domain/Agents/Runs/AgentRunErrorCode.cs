using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Agents.Runs;

/// <summary>
/// Defines why a run failed. <c>providerUnavailable</c> and <c>unexpectedError</c> make the case show that the AI is
/// unavailable. <c>caseChanged</c> does not: a signal of the case changed during the run, so a new triage run follows.
/// <c>noDecision</c> does not either: the planner ended without a write action, also after its decision turn.
/// <c>timeLimit</c> and <c>outputLimit</c> end a chat turn that ran too long or wrote too much.
/// The camelCase values reach the database, so never rename one in place.
/// </summary>
public sealed class AgentRunErrorCode : Enumeration<AgentRunErrorCode>
{
    public static readonly AgentRunErrorCode ProviderUnavailable = new(value: "providerUnavailable");
    public static readonly AgentRunErrorCode UnexpectedError = new(value: "unexpectedError");
    public static readonly AgentRunErrorCode CaseChanged = new(value: "caseChanged");
    public static readonly AgentRunErrorCode NoDecision = new(value: "noDecision");
    public static readonly AgentRunErrorCode TimeLimit = new(value: "timeLimit");
    public static readonly AgentRunErrorCode OutputLimit = new(value: "outputLimit");

    private AgentRunErrorCode(string value)
        : base(value)
    {
    }
}
