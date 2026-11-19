using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Agents.Runs;

/// <summary>
/// Defines why a run ended without work: <c>caseNotOpen</c> when the case was resolved, closed, or gone before the run
/// started, and <c>nothingToReview</c> when the open case had no open signal that still needed a review. The camelCase
/// values reach the database, so never rename one in place.
/// </summary>
public sealed class AgentRunSkipReason : Enumeration<AgentRunSkipReason>
{
    public static readonly AgentRunSkipReason CaseNotOpen = new(value: "caseNotOpen");
    public static readonly AgentRunSkipReason NothingToReview = new(value: "nothingToReview");

    private AgentRunSkipReason(string value)
        : base(value)
    {
    }
}
