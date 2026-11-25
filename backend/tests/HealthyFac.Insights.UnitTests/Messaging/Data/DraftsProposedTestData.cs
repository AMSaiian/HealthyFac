using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Insights.UnitTests.Messaging.Data;

public static class DraftsProposedTestData
{
    private static readonly Guid TargetMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000a1");
    private static readonly Guid CreateDraftId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000a2");

    public static TheoryData<DraftsProposed, string> InvalidMessages =>
        new()
        {
            {
                Ingest(isMachineFile: false, CreateMachines(count: 21)),
                DraftMessageErrors.Validation.MachinesCount
            },
            {
                Ingest(isMachineFile: false, metrics: CreateMetrics(count: 101)),
                DraftMessageErrors.Validation.MetricsCount
            },
            {
                Ingest(isMachineFile: false, statuses: CreateStatuses(count: 101)),
                DraftMessageErrors.Validation.StatusesCount
            },
            {
                TwoCreateDraftsWithMetrics(count: 101),
                DraftMessageErrors.Validation.MetricsCount
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric(min: 5, max: 5)]),
                DraftMessageErrors.Validation.RangeInvalid
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric(warningLow: 1, alarmLow: 5)]),
                DraftMessageErrors.Validation.WarningLowBelowAlarmLow
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric(warningHigh: 95, alarmHigh: 90)]),
                DraftMessageErrors.Validation.WarningHighAboveAlarmHigh
            },
            {
                Ingest(
                    isMachineFile: false,
                    metrics: [CreateMetric(valueKind: DraftMessageLimits.ValueKinds.Boolean, alarmHigh: 1)]),
                DraftMessageErrors.Validation.LimitsOnlyForNumeric
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric(behavior: "wave")]),
                DraftMessageErrors.Validation.BehaviorInvalid
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric(states: ["on"])]),
                DraftMessageErrors.Validation.StatesInvalid
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric(states: ["on", "ON"])]),
                DraftMessageErrors.Validation.StatesInvalid
            },
            {
                Ingest(isMachineFile: true, [CreateMachine(TargetMachineId), CreateMachine(TargetMachineId)]),
                DraftMessageErrors.Validation.MachineFileNeedsOneTarget
            },
            {
                Ingest(isMachineFile: true, [CreateMachine(targetMachineId: null)]),
                DraftMessageErrors.Validation.MachineFileNeedsOneTarget
            },
            {
                Ingest(isMachineFile: false, [CreateMachine(hasContent: false)]),
                DraftMessageErrors.Validation.MachineWithoutContent
            },
            {
                Ingest(isMachineFile: false, [CreateMachine(TargetMachineId, hasContent: false)]),
                DraftMessageErrors.Validation.UpdateWithoutContent
            },
            {
                Ingest(isMachineFile: false),
                DraftMessageErrors.Validation.MessageWithoutContent
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric()]) with { Source = null! },
                DraftMessageErrors.Validation.SourceRequired
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric()]) with { AttemptId = null },
                DraftMessageErrors.Validation.IdRequired
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric()]) with { IsMachineFile = null },
                DraftMessageErrors.Validation.IsMachineFileRequired
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric()]) with { ChatId = Guid.CreateVersion7() },
                DraftMessageErrors.Validation.FieldsOfAnotherSource
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric()]) with { ChatMessageId = Guid.CreateVersion7() },
                DraftMessageErrors.Validation.FieldsOfAnotherSource
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric()]) with { FileId = Guid.Empty },
                DraftMessageErrors.Validation.IdRequired
            },
            {
                Ingest(isMachineFile: false, metrics: [CreateMetric()]) with { QueuedAt = null },
                DraftMessageErrors.Validation.QueuedAtRequired
            },
            {
                Chat(metrics: [CreateMetric()]) with { ChatMessageId = null },
                DraftMessageErrors.Validation.IdRequired
            },
            {
                Chat(metrics: [CreateMetric()]) with { FileId = Guid.CreateVersion7() },
                DraftMessageErrors.Validation.FieldsOfAnotherSource
            },
            {
                Chat(metrics: [CreateMetric()]) with { IsMachineFile = false },
                DraftMessageErrors.Validation.FieldsOfAnotherSource
            },
            {
                Chat(metrics: [CreateMetric() with { MachineId = Guid.Empty }]),
                DraftMessageErrors.Validation.IdRequired
            },
            {
                Chat(
                    [CreateMachine() with { DraftId = CreateDraftId }],
                    [CreateMetric() with { MachineId = null, MachineDraftId = Guid.Empty }]),
                DraftMessageErrors.Validation.IdRequired
            },
            {
                Chat(metrics: [CreateMetric()]) with { ChatId = null },
                DraftMessageErrors.Validation.IdRequired
            },
            {
                Chat(metrics: [CreateMetric()]) with { AttemptId = Guid.CreateVersion7() },
                DraftMessageErrors.Validation.FieldsOfAnotherSource
            },
            {
                Chat(metrics: [CreateMetric()]) with { QueuedAt = DateTime.UtcNow },
                DraftMessageErrors.Validation.FieldsOfAnotherSource
            },
            {
                Chat(metrics: [CreateMetric() with { MachineId = null }]),
                DraftMessageErrors.Validation.DefinitionMachineInvalid
            },
            {
                Chat(
                    [CreateMachine() with { DraftId = CreateDraftId }],
                    [CreateMetric() with { MachineDraftId = CreateDraftId }]),
                DraftMessageErrors.Validation.DefinitionMachineInvalid
            },
            {
                Chat(metrics: [CreateMetric() with { MachineId = null, MachineDraftId = Guid.CreateVersion7() }]),
                DraftMessageErrors.Validation.MachineDraftUnknown
            },
            {
                Chat(
                    [CreateMachine(TargetMachineId) with { DraftId = CreateDraftId }],
                    [CreateMetric() with { MachineId = null, MachineDraftId = CreateDraftId }]),
                DraftMessageErrors.Validation.MachineDraftUnknown
            },
        };

    public static TheoryData<DraftsProposed> ValidMessages =>
        new()
        {
            Ingest(isMachineFile: false, CreateMachines(count: 20)),
            Ingest(isMachineFile: false, metrics: CreateMetrics(count: 100)),
            TwoCreateDraftsWithMetrics(count: 60),
            Ingest(isMachineFile: false, metrics: [CreateMetric(min: 5, max: 6)]),
            Ingest(isMachineFile: false, metrics: [CreateMetric(states: ["on", "off"])]),
            Ingest(isMachineFile: true, [CreateMachine(TargetMachineId)]),
            Ingest(isMachineFile: true, metrics: [CreateMetric()]),
            Chat(metrics: [CreateMetric()]),
            Chat(
                [CreateMachine(hasContent: false) with { DraftId = CreateDraftId }],
                [CreateMetric() with { MachineId = null, MachineDraftId = CreateDraftId }]),
            Chat([CreateMachine(TargetMachineId)]),
        };

    private static DraftsProposed Ingest(
        bool isMachineFile,
        IReadOnlyList<ProposedMachine>? machines = null,
        IReadOnlyList<ProposedMetric>? metrics = null,
        IReadOnlyList<ProposedStatus>? statuses = null
    ) =>
        new()
        {
            Source = ProposalSource.Ingest,
            AttemptId = Guid.CreateVersion7(),
            FileId = Guid.CreateVersion7(),
            CompanyId = Guid.CreateVersion7(),
            IsMachineFile = isMachineFile,
            QueuedAt = new DateTime(year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, DateTimeKind.Utc),
            Machines = machines ?? [],
            Metrics = metrics ?? [],
            Statuses = statuses ?? [],
        };

    private static DraftsProposed Chat(
        IReadOnlyList<ProposedMachine>? machines = null,
        IReadOnlyList<ProposedMetric>? metrics = null
    ) =>
        new()
        {
            Source = ProposalSource.Chat,
            ChatId = Guid.CreateVersion7(),
            ChatMessageId = Guid.CreateVersion7(),
            CompanyId = Guid.CreateVersion7(),
            Machines = machines ?? [],
            Metrics = metrics ?? [],
        };

    private static List<ProposedMachine> CreateMachines(int count) =>
        [.. Enumerable.Range(start: 0, count).Select(_ => CreateMachine())];

    private static ProposedMachine CreateMachine(Guid? targetMachineId = null, bool hasContent = true) =>
        new()
        {
            DraftId = Guid.CreateVersion7(),
            TargetMachineId = targetMachineId,
            Name = hasContent ? "AX-200 Rotary Pump" : null,
        };

    private static DraftsProposed TwoCreateDraftsWithMetrics(int count)
    {
        var first = CreateMachine();
        var second = CreateMachine();
        return Ingest(
            isMachineFile: false, [first, second],
            [
                .. CreateMetrics(count)
                    .Select(metric => metric with { MachineId = null, MachineDraftId = first.DraftId }),
                .. CreateMetrics(count)
                    .Select(metric => metric with { MachineId = null, MachineDraftId = second.DraftId }),
            ]);
    }

    private static List<ProposedMetric> CreateMetrics(int count) =>
        [.. Enumerable.Range(start: 0, count).Select(_ => CreateMetric())];

    private static List<ProposedStatus> CreateStatuses(int count) =>
    [
        .. Enumerable.Range(start: 0, count)
            .Select(_ => new ProposedStatus
            {
                DraftId = Guid.CreateVersion7(),
                MachineId = TargetMachineId,
                Code = "E104",
            }),
    ];

    private static ProposedMetric CreateMetric(
        double? min = null,
        double? max = null,
        IReadOnlyList<string>? states = null,
        string? valueKind = null,
        double? warningLow = null,
        double? warningHigh = null,
        double? alarmLow = null,
        double? alarmHigh = null,
        string? behavior = null
    ) =>
        new()
        {
            DraftId = Guid.CreateVersion7(),
            MachineId = TargetMachineId,
            Key = "motor.temperature",
            ValueKind = valueKind,
            Min = min,
            Max = max,
            States = states,
            WarningLow = warningLow,
            WarningHigh = warningHigh,
            AlarmLow = alarmLow,
            AlarmHigh = alarmHigh,
            Behavior = behavior,
        };
}
