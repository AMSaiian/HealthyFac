using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;

namespace HealthyFac.Telemetry.UnitTests.DefinitionDrafts.Data;

public static class OperatingPhaseDraftTestData
{
    public static readonly Guid StatusDraftId = Guid.Parse("5c1b6f2e-3a47-4d0e-9b58-0f6a2d7e8c11");
    public static readonly Guid StatusDefinitionId = Guid.Parse("8e2a4d90-61b3-4c75-a1f2-3b9d5c7e0a22");

    public static OperatingPhaseDraft CreateWithTwoConditionsOnOneStatusDraft() =>
        OperatingPhaseDraft.Propose(
            ProposalEntryTestData.DraftId,
            ProposalEntryTestData.CompanyId,
            ProposalEntryTestData.AttemptId,
            ProposalEntryTestData.FileId,
            Guid.Parse("2d4f6a81-9c03-4e5b-8a17-6b0c3e9d1f33"),
            tentativeMachineId: null,
            new PhaseDraftValues { Code = "cutting" },
            [StatusCondition(), StatusCondition()],
            []);

    private static PhaseDraftConditionValues StatusCondition() =>
        new() { StatusDraftId = StatusDraftId, Operator = "active" };
}
