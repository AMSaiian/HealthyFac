using HealthyFac.Core.Domain.Drafts;

namespace HealthyFac.Core.UnitTests.Drafts.Data;

public static class DraftTestData
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid AttemptId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000002");
    public static readonly Guid FileId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000003");
    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000004");
    public static readonly Guid MachineDraftId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000005");

    public static readonly DateTime QueuedAt =
        new(year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static MachineDraft CreateMachineDraft(Guid? targetMachineId = null) =>
        new(
            Guid.CreateVersion7(),
            CompanyId,
            AttemptId,
            FileId,
            QueuedAt,
            targetMachineId,
            name: "AX-200",
            serialNumber: null,
            description: null);

    public static MetricDefinitionDraft CreateMetricForMachine() =>
        MetricDefinitionDraft.ForMachine(
            Guid.CreateVersion7(), CompanyId, AttemptId, FileId, MachineId,
            new MetricDraftValues { Key = "motor.temp" });

    public static MetricDefinitionDraft CreateMetricForMachineDraft() =>
        MetricDefinitionDraft.ForMachineDraft(
            Guid.CreateVersion7(),
            CompanyId,
            AttemptId,
            FileId,
            MachineDraftId,
            new MetricDraftValues { Key = "motor.temp" });

    public static StatusDefinitionDraft CreateStatusForMachine() =>
        StatusDefinitionDraft.ForMachine(
            Guid.CreateVersion7(), CompanyId, AttemptId, FileId, MachineId, new StatusDraftValues { Code = "E104" });

    public static StatusDefinitionDraft CreateStatusForMachineDraft() =>
        StatusDefinitionDraft.ForMachineDraft(
            Guid.CreateVersion7(), CompanyId, AttemptId, FileId, MachineDraftId,
            new StatusDraftValues { Code = "E104" });
}
