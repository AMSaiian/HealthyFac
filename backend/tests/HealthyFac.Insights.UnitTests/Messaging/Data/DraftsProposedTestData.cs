using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Insights.UnitTests.Messaging.Data;

public static class DraftsProposedTestData
{
    private static readonly Guid TargetMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000a1");

    public static TheoryData<DraftsProposed, string> InvalidMessages =>
        new()
        {
            {
                CreateMessage(isMachineFile: false, CreateMachines(count: 21)),
                DraftMessageErrors.Validation.MachinesCount
            },
            {
                CreateMessage(isMachineFile: false, [CreateMachine(metrics: CreateMetrics(count: 101))]),
                DraftMessageErrors.Validation.MetricsCount
            },
            {
                CreateMessage(isMachineFile: false, [CreateMachine(metrics: [CreateMetric(min: 5, max: 5)])]),
                DraftMessageErrors.Validation.RangeInvalid
            },
            {
                CreateMessage(
                    isMachineFile: false, [CreateMachine(metrics: [CreateMetric(warningLow: 1, alarmLow: 5)])]),
                DraftMessageErrors.Validation.WarningLowBelowAlarmLow
            },
            {
                CreateMessage(
                    isMachineFile: false, [CreateMachine(metrics: [CreateMetric(warningHigh: 95, alarmHigh: 90)])]),
                DraftMessageErrors.Validation.WarningHighAboveAlarmHigh
            },
            {
                CreateMessage(
                    isMachineFile: false,
                    [
                        CreateMachine(
                            metrics:
                            [
                                CreateMetric(valueKind: DraftMessageLimits.ValueKinds.Boolean, alarmHigh: 1),
                            ]),
                    ]),
                DraftMessageErrors.Validation.LimitsOnlyForNumeric
            },
            {
                CreateMessage(isMachineFile: false, [CreateMachine(metrics: [CreateMetric(behavior: "wave")])]),
                DraftMessageErrors.Validation.BehaviorInvalid
            },
            {
                CreateMessage(isMachineFile: false, [CreateMachine(metrics: [CreateMetric(states: ["on"])])]),
                DraftMessageErrors.Validation.StatesInvalid
            },
            {
                CreateMessage(isMachineFile: false, [CreateMachine(metrics: [CreateMetric(states: ["on", "ON"])])]),
                DraftMessageErrors.Validation.StatesInvalid
            },
            {
                CreateMessage(isMachineFile: true, [CreateMachine(TargetMachineId), CreateMachine(TargetMachineId)]),
                DraftMessageErrors.Validation.MachineFileNeedsOneTarget
            },
            {
                CreateMessage(isMachineFile: true, [CreateMachine(targetMachineId: null)]),
                DraftMessageErrors.Validation.MachineFileNeedsOneTarget
            },
            {
                CreateMessage(isMachineFile: false, [CreateMachine(hasContent: false)]),
                DraftMessageErrors.Validation.MachineWithoutContent
            },
        };

    public static TheoryData<DraftsProposed> ValidMessages =>
    [
        CreateMessage(isMachineFile: false, CreateMachines(count: 20)),
        CreateMessage(isMachineFile: false, [CreateMachine(metrics: CreateMetrics(count: 100))]),
        CreateMessage(isMachineFile: false, [CreateMachine(metrics: [CreateMetric(min: 5, max: 6)])]),
        CreateMessage(isMachineFile: false, [CreateMachine(metrics: [CreateMetric(states: ["on", "off"])])]),
        CreateMessage(isMachineFile: true, [CreateMachine(TargetMachineId)]),
    ];

    private static DraftsProposed CreateMessage(bool isMachineFile, IReadOnlyList<ProposedMachine> machines) =>
        new()
        {
            AttemptId = Guid.CreateVersion7(),
            FileId = Guid.CreateVersion7(),
            CompanyId = Guid.CreateVersion7(),
            IsMachineFile = isMachineFile,
            QueuedAt = new DateTime(year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, DateTimeKind.Utc),
            Machines = machines,
        };

    private static List<ProposedMachine> CreateMachines(int count) =>
        [.. Enumerable.Range(start: 0, count).Select(_ => CreateMachine())];

    private static ProposedMachine CreateMachine(
        Guid? targetMachineId = null,
        IReadOnlyList<ProposedMetric>? metrics = null,
        bool hasContent = true
    ) =>
        new()
        {
            DraftId = Guid.CreateVersion7(),
            TargetMachineId = targetMachineId,
            Name = hasContent ? "AX-200 Rotary Pump" : null,
            Metrics = metrics ?? [],
            Statuses = [],
        };

    private static List<ProposedMetric> CreateMetrics(int count) =>
        [.. Enumerable.Range(start: 0, count).Select(_ => CreateMetric())];

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
