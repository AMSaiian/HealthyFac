using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

public static class PhaseDraftTestData
{
    public const string PhaseCode = "cutting";

    public static TheoryData<string, Func<FlowIds, Guid>> UsedDrafts => new()
    {
        { "a metric draft of a limit row", ids => ids.MetricId },
        { "a status draft of a condition", ids => ids.StatusId },
    };

    public static ProposedPhase ProposedPhase(FlowIds ids) =>
        new()
        {
            DraftId = ids.PhaseId,
            SourceLocation = "p.1",
            Code = PhaseCode,
            Name = "Cutting",
            Priority = 0,
            IsTracked = true,
            Conditions =
            [
                new ProposedPhaseCondition { StatusDraftId = ids.StatusId, Operator = "active" },
            ],
            Limits =
            [
                new ProposedPhaseLimit { MetricDraftId = ids.MetricId, AlarmHigh = 90, WarningHigh = 80 },
            ],
        };
}
