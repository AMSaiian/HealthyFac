using System.Text.Json;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Machines;
using HealthyFac.MachineSimulator.Scenarios.Machines.Snapshots;
using HealthyFac.Seeder.Ingest;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.TelemetryHistory;
using HealthyFac.Seeder.TelemetryHistory.Simulation;
using HealthyFac.Seeder.UnitTests.SeedFiles.Data;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Seeder.UnitTests.TelemetryHistory.Data;

public static class HistorySimulationTestData
{
    public const string SpindleDrainKey = "lathe-spindle";
    public const string CabinetMetric = "cabinet.temperature";
    public const string CoolantMetric = "coolant.level";
    public const string DoorMetric = "door.open";
    public const string SpindleSpeedMetric = "spindle.speed";
    public const string OverrideCode = "A101";

    public static readonly DateTimeOffset Anchor =
        new(year: 2027, month: 6, day: 2, hour: 8, minute: 0, second: 0, TimeSpan.Zero);

    /// <summary>The window of 12 hours before the anchor.</summary>
    public static readonly DateTime WindowStart = Anchor.UtcDateTime.AddHours(value: -12);

    public static readonly TimeSpan TenSeconds = TimeSpan.FromSeconds(seconds: 10);
    public static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(seconds: 1);

    private static readonly TimeZoneInfo Zone = CommittedSeedTestData.Zone;

    public static HistoryEvents NoEvents() => Events();

    public static HistoryEvents Events(
        DrainSeed[]? drains = null,
        StepSeed[]? steps = null,
        GapSeed[]? gaps = null,
        OverrideSeed[]? overrides = null
    ) =>
        new(drains ?? [], steps ?? [], gaps ?? [], overrides ?? [], Anchor, Zone);

    public static DrainSeed Drain(double drainHours) =>
        new()
        {
            Key = SpindleDrainKey,
            Machine = "lathe",
            Module = "spindle",
            From = RelativeMoment.Parse(text: "h-12"),
            DrainHours = drainHours,
            Maintenance = new MaintenanceSeed
            {
                By = "maksym",
                From = RelativeMoment.Parse(text: "h-6"),
                Until = RelativeMoment.Parse(text: "h-5"),
            },
        };

    public static StepSeed Step(string from, string until) =>
        new()
        {
            Key = "cabinet-step",
            Machine = "lathe",
            Metric = CabinetMetric,
            Offset = 5,
            From = RelativeMoment.Parse(from),
            Until = RelativeMoment.Parse(until),
        };

    public static GapSeed Gap(string from, string until) =>
        new()
        {
            Key = "lathe-gap",
            Machine = "lathe",
            From = RelativeMoment.Parse(from),
            Until = RelativeMoment.Parse(until),
        };

    public static OverrideSeed Override(string from, string until) =>
        new()
        {
            Key = "lathe-override",
            Machine = "lathe",
            Values = new Dictionary<string, JsonElement>
            {
                [CoolantMetric] = JsonSerializer.SerializeToElement(value: 42),
                [DoorMetric] = JsonSerializer.SerializeToElement(value: true),
            },
            Codes = [OverrideCode],
            From = RelativeMoment.Parse(from),
            Until = RelativeMoment.Parse(until),
        };

    public static StateHistoryEntry StateChange(MachineState state, TimeSpan afterWindowStart) =>
        new() { State = state, StartedAt = WindowStart + afterWindowStart };

    public static HistorySimulation Create(
        HistoryEvents events,
        TimeSpan interval,
        IReadOnlyList<StateHistoryEntry>? stateChanges = null
    ) =>
        new(Snapshot(), WindowStart, interval, new Random(Seed: 8), stateChanges ?? [], events);

    public static bool AreEqual(ScenarioSample left, ScenarioSample right, params string[] exceptKeys) =>
        left.StatusCodes.SequenceEqual(right.StatusCodes) && AreValuesEqual(left, right, exceptKeys);

    public static bool AreValuesEqual(ScenarioSample left, ScenarioSample right, params string[] exceptKeys) =>
        left.IsRunning == right.IsRunning
        && left.Values.Keys.OrderBy(key => key).SequenceEqual(right.Values.Keys.OrderBy(key => key))
        && left.Values.Where(pair => !exceptKeys.Contains(pair.Key))
            .All(pair => Equals(pair.Value, right.Values[pair.Key]));

    private static MachineSnapshot Snapshot() =>
        new()
        {
            MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e01"),
            CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e02"),
            Name = "Lathe",
            SerialNumber = ScenarioBinding.CncSerialNumber,
            GatewayId = "abcdefghijklmnop",
            ExpectedGoLiveOn = new DateOnly(year: 2027, month: 5, day: 20),
            Zone = Zone,
            StateHistory =
            [
                new StateHistoryEntry { State = MachineState.Operational, StartedAt = WindowStart.AddDays(value: -30) },
            ],
            IsRegistered = true,
            Definitions = LatheDefinitions(),
        };

    private static MachineDefinitions LatheDefinitions()
    {
        var draft = CommittedSeedTestData.CommittedFolder()
            .Read<IngestSetFile>(relativePath: "ingest/set-1.json")
            .Entries
            .SelectMany(entry => entry.DraftsProposed.Machines)
            .Single(machine => machine.SerialNumber == ScenarioBinding.CncSerialNumber);

        return new MachineDefinitions
        {
            Metrics = draft.Metrics
                .Select(metric => new MetricInfo
                {
                    Key = metric.Key!,
                    ValueKind = MetricValueKind.FromValue(metric.ValueKind!),
                    Min = metric.Min,
                    Max = metric.Max,
                    States = metric.States,
                })
                .ToList(),
            StatusCodes = draft.Statuses.Select(status => status.Code!).ToList(),
        };
    }
}
