using System.Text.Json;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;

namespace HealthyFac.Insights.UnitTests.Cases.Data;

public static class CaseOutcomeTestData
{
    public const string WorkOrderTitle = "Replace the coolant filter";
    public const string FinalText = "The filter pressure rises for three days.";
    public const string CloseReason = "Fixed by the operator";

    public static readonly Guid WorkOrderId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a01");

    public static AgentRun CreateCompletedRun(AgentRunVerdict? verdict, string? reason = FinalText)
    {
        var run = new AgentRun(AgentRole.Triage, CaseTestData.CaseId, ingestAttemptId: null, CaseTestData.Now);
        run.Claim(CaseTestData.Now, CaseTestData.Now.AddMinutes(value: 5));
        run.Complete(CaseTestData.Now, verdict, reason, inputTokens: 0, outputTokens: 0);

        return run;
    }

    public static AgentRun CreateFailedRun(AgentRunErrorCode errorCode)
    {
        var run = new AgentRun(AgentRole.Planner, CaseTestData.CaseId, ingestAttemptId: null, CaseTestData.Now);
        run.Claim(CaseTestData.Now, CaseTestData.Now.AddMinutes(value: 5));
        run.Fail(CaseTestData.Now, errorCode);

        return run;
    }

    public static AgentAction CreateProposeAction() =>
        new(
            Guid.CreateVersion7(),
            callId: "call-1",
            AgentActionKind.ProposeWorkOrder,
            JsonSerializer.Serialize(
                new ProposeWorkOrderPayload
                {
                    SignalIds = [],
                    Title = WorkOrderTitle,
                    Priority = "high",
                    Category = "corrective",
                    EstimateMinutes = 60,
                },
                JsonSerializerOptions.Web));

    public static AgentAction CreateLinkAction() =>
        new(
            Guid.CreateVersion7(),
            callId: "call-1",
            AgentActionKind.LinkWorkOrder,
            JsonSerializer.Serialize(
                new LinkWorkOrderPayload { SignalIds = [], WorkOrderId = WorkOrderId },
                JsonSerializerOptions.Web));

    public static AgentAction CreateCloseAction() =>
        new(
            Guid.CreateVersion7(),
            callId: "call-1",
            AgentActionKind.CloseCase,
            JsonSerializer.Serialize(new CloseCasePayload { Reason = CloseReason }, JsonSerializerOptions.Web));
}
