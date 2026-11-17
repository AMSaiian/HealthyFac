using HealthyFac.Core.Domain.Machines;
using HealthyFac.MachineSimulator.Machines;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.MachineSimulator.UnitTests.Simulation.Data;

public static class SimulationEngineTestData
{
    public const int Seed = 20261116;

    public static readonly DateTimeOffset Start = new(
        year: 2026, month: 3, day: 11, hour: 8, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly TimeZoneInfo Kyiv = TimeZoneInfo.FindSystemTimeZoneById(id: "Europe/Kyiv");

    public static readonly MachineDefinitions LevelDefinitions = new()
    {
        Metrics = [new MetricInfo { Key = "level", ValueKind = MetricValueKind.Numeric, Min = 0, Max = 10 }],
        StatusCodes = [],
    };

    public static readonly MachineDefinitions LevelAndPressureDefinitions = new()
    {
        Metrics =
        [
            .. LevelDefinitions.Metrics,
            new MetricInfo { Key = "pressure", ValueKind = MetricValueKind.Numeric, Min = 0, Max = 5 },
        ],
        StatusCodes = [],
    };

    public static readonly MachineDefinitions CncDefinitions = new()
    {
        Metrics = [new MetricInfo { Key = "spindle.speed", ValueKind = MetricValueKind.Numeric, Min = 0, Max = 4000 }],
        StatusCodes = ["S001", "S002", "A101", "A111"],
    };

    public static MachineSnapshot Snapshot(
        string serialNumber,
        MachineDefinitions definitions,
        bool isRegistered = true,
        Guid? machineId = null
    ) =>
        new()
        {
            MachineId = machineId ?? Guid.CreateVersion7(),
            CompanyId = Guid.CreateVersion7(),
            Name = "Machine",
            SerialNumber = serialNumber,
            GatewayId = "abcdefghijklmnop",
            ExpectedGoLiveOn = new DateOnly(year: 2026, month: 1, day: 1),
            Zone = Kyiv,
            StateHistory =
            [
                new StateHistoryEntry
                    { State = MachineState.Operational, StartedAt = Start.UtcDateTime.AddDays(value: -30) },
            ],
            IsRegistered = isRegistered,
            Definitions = definitions,
        };

    public static readonly IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, double>> NoCounterStarts =
        new Dictionary<Guid, IReadOnlyDictionary<string, double>>();
}
