using HealthyFac.Core.Domain.Machines;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Machines;
using HealthyFac.MachineSimulator.Scenarios.Machines.Snapshots;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.MachineSimulator.UnitTests.Machines.Data;

public static class SimulatedMachineTestData
{
    public const string GenericSerialNumber = "GEN-1";
    public const string CncSerialNumber = ScenarioBinding.CncSerialNumber;
    public const string PumpSerialNumber = ScenarioBinding.PumpSerialNumber;

    public static readonly TimeSpan DrainDuration = TimeSpan.FromMinutes(minutes: 3);
    public static readonly TimeSpan LongDrainDuration = TimeSpan.FromHours(hours: 8);
    public static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(seconds: 1);

    /// <summary>23:30 UTC on 2026-03-10 is already 2026-03-11 in Kyiv.</summary>
    public static readonly DateTime Now = new(
        year: 2026, month: 3, day: 10, hour: 23, minute: 30, second: 0, DateTimeKind.Utc);

    public static readonly DateTime Discovery = Now.AddHours(value: -1);

    public static readonly DateOnly TodayInKyiv = new(year: 2026, month: 3, day: 11);

    public static readonly TimeZoneInfo Kyiv = TimeZoneInfo.FindSystemTimeZoneById(id: "Europe/Kyiv");

    public static readonly MachineDefinitions GenericDefinitions = new()
    {
        Metrics = [new MetricInfo { Key = "level", ValueKind = MetricValueKind.Numeric, Min = 0, Max = 10 }],
        StatusCodes = [],
    };

    public static TheoryData<string, DateOnly, bool> GoLiveGates =>
        new()
        {
            { "GoLiveIsToday", TodayInKyiv, true },
            { "GoLiveIsTomorrow", TodayInKyiv.AddDays(value: 1), false },
        };

    public static MachineSnapshot Snapshot(
        string serialNumber = GenericSerialNumber,
        DateOnly? goLiveOn = null,
        bool isRegistered = true,
        MachineDefinitions? definitions = null,
        params StateHistoryEntry[] history
    ) =>
        new()
        {
            MachineId = Guid.CreateVersion7(),
            CompanyId = Guid.CreateVersion7(),
            Name = "Machine",
            SerialNumber = serialNumber,
            GatewayId = "abcdefghijklmnop",
            ExpectedGoLiveOn = goLiveOn ?? TodayInKyiv,
            Zone = Kyiv,
            StateHistory = history.Length == 0
                ? [Row(MachineState.Operational, Now.AddDays(value: -30))]
                : history,
            IsRegistered = isRegistered,
            Definitions = definitions ?? GenericDefinitions,
        };

    public static MachineSnapshot WithHistory(MachineSnapshot snapshot, params StateHistoryEntry[] history) =>
        snapshot with { StateHistory = history };

    public static SimulatedMachine Create(MachineSnapshot snapshot) =>
        new(snapshot, Discovery, DrainDuration, new Random(Seed));

    public const int Seed = 20261116;

    public static StateHistoryEntry Row(MachineState state, DateTime startedAt) =>
        new() { State = state, StartedAt = startedAt };
}
