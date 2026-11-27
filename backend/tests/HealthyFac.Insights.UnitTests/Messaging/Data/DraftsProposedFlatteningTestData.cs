using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Insights.UnitTests.Messaging.Data;

public static class DraftsProposedFlatteningTestData
{
    public const string MachineName = "AX-200 Rotary Pump";

    public static readonly Guid FileMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000b1");

    public static ProposedMachineGroup Group(bool isWithValue, bool isWithDefinitions) =>
        new()
        {
            DraftId = Guid.CreateVersion7(),
            TargetMachineId = FileMachineId,
            Name = isWithValue ? MachineName : null,
            Metrics = isWithDefinitions
                ? [new ProposedMetric { DraftId = Guid.CreateVersion7(), Key = "motor.temperature" }]
                : [],
            Statuses = isWithDefinitions
                ? [new ProposedStatus { DraftId = Guid.CreateVersion7(), Code = "E104" }]
                : [],
            Phases = isWithDefinitions
                ? [new ProposedPhase { DraftId = Guid.CreateVersion7(), Code = "idle" }]
                : [],
        };
}
