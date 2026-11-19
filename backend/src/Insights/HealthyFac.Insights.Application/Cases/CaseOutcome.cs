using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Tools.Writes;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Application.Cases;

/// <summary>What the agents decided for a case.</summary>
public sealed class CaseOutcomeKind : Enumeration<CaseOutcomeKind>
{
    public static readonly CaseOutcomeKind ProposedWorkOrder = new(value: "proposedWorkOrder");

    public static readonly CaseOutcomeKind WorkOrderCreated = new(value: "workOrderCreated");

    public static readonly CaseOutcomeKind ProposalDismissed = new(value: "proposalDismissed");

    public static readonly CaseOutcomeKind LinkedWorkOrder = new(value: "linkedWorkOrder");

    public static readonly CaseOutcomeKind Closed = new(value: "closed");

    public static readonly CaseOutcomeKind Watched = new(value: "watched");

    public static readonly CaseOutcomeKind Dismissed = new(value: "dismissed");

    public static readonly CaseOutcomeKind Escalated = new(value: "escalated");

    public static readonly CaseOutcomeKind NoDecision = new(value: "noDecision");

    public static readonly CaseOutcomeKind AiUnavailable = new(value: "aiUnavailable");

    public static readonly CaseOutcomeKind None = new(value: "none");

    private CaseOutcomeKind(string value)
        : base(value)
    {
    }
}

/// <summary>
/// Decides the outcome of a case. The latest committed action wins, because it changed something. A proposal follows
/// its draft: created as a work order, or dismissed. Without an action,
/// an unavailable AI comes next, then the verdict or the error of the last triage or planner run.
/// </summary>
public static class CaseOutcomes
{
    /// <summary>Builds the outcome.</summary>
    /// <param name="latestAction">The latest committed action of the case, or <see langword="null"/>.</param>
    /// <param name="decisionRun">The last final triage or planner run of the case, or <see langword="null"/>.</param>
    /// <param name="isAiUnavailable">Tells whether the last run of the case left the AI unavailable.</param>
    /// <param name="reason">The final text of the last triage or planner run that has one.</param>
    /// <returns>The outcome.</returns>
    public static CaseOutcomeResponse Resolve(
        AgentAction? latestAction,
        AgentRun? decisionRun,
        bool isAiUnavailable,
        string? reason
    )
    {
        if (latestAction is not null && FromAction(latestAction, reason) is { } fromAction)
        {
            return fromAction;
        }

        if (isAiUnavailable)
        {
            return new CaseOutcomeResponse { Kind = CaseOutcomeKind.AiUnavailable, Reason = reason };
        }

        return new CaseOutcomeResponse { Kind = KindOf(decisionRun), Reason = reason };
    }

    private static CaseOutcomeResponse? FromAction(AgentAction action, string? reason)
    {
        if (action.Kind == AgentActionKind.ProposeWorkOrder)
        {
            if (action.FollowUp == AgentActionFollowUp.Dismissed)
            {
                return new CaseOutcomeResponse
                {
                    Kind = CaseOutcomeKind.ProposalDismissed,
                    Title = ActionBuffering.Deserialize<ProposeWorkOrderPayload>(action).Title,
                    Reason = reason,
                };
            }

            if (action.FollowUp == AgentActionFollowUp.Applied)
            {
                return new CaseOutcomeResponse
                {
                    Kind = CaseOutcomeKind.WorkOrderCreated,
                    WorkOrderId = action.FollowUpWorkOrderId,
                    Title = ActionBuffering.Deserialize<ProposeWorkOrderPayload>(action).Title,
                    Reason = reason,
                };
            }

            return new CaseOutcomeResponse
            {
                Kind = CaseOutcomeKind.ProposedWorkOrder,
                WorkOrderDraftId = action.Id,
                Title = ActionBuffering.Deserialize<ProposeWorkOrderPayload>(action).Title,
                Reason = reason,
            };
        }

        if (action.Kind == AgentActionKind.LinkWorkOrder)
        {
            return new CaseOutcomeResponse
            {
                Kind = CaseOutcomeKind.LinkedWorkOrder,
                WorkOrderId = ActionBuffering.Deserialize<LinkWorkOrderPayload>(action).WorkOrderId,
                Reason = reason,
            };
        }

        if (action.Kind == AgentActionKind.CloseCase)
        {
            return new CaseOutcomeResponse
            {
                Kind = CaseOutcomeKind.Closed,
                Reason = ActionBuffering.Deserialize<CloseCasePayload>(action).Reason,
            };
        }

        return null;
    }

    private static CaseOutcomeKind KindOf(AgentRun? decisionRun)
    {
        if (decisionRun is null)
        {
            return CaseOutcomeKind.None;
        }

        if (decisionRun.ErrorCode == AgentRunErrorCode.NoDecision)
        {
            return CaseOutcomeKind.NoDecision;
        }

        if (decisionRun.Verdict == AgentRunVerdict.Dismiss)
        {
            return CaseOutcomeKind.Dismissed;
        }

        if (decisionRun.Verdict == AgentRunVerdict.Watch)
        {
            return CaseOutcomeKind.Watched;
        }

        return decisionRun.Verdict == AgentRunVerdict.Escalate ? CaseOutcomeKind.Escalated : CaseOutcomeKind.None;
    }
}
