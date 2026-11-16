using HealthyFac.MachineSimulator.Machines;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.MachineSimulator.UnitTests.Publishing.Data;

public static class MachinePayloadBuilderTestData
{
    public static readonly DateTime SampledAt = new(
        year: 2026, month: 3, day: 10, hour: 23, minute: 30, second: 5, DateTimeKind.Utc);

    public static readonly MachineDefinitions Definitions = new()
    {
        Metrics =
        [
            new MetricInfo { Key = "speed", ValueKind = MetricValueKind.Numeric, Min = 0, Max = 100, States = null },
            new MetricInfo
                { Key = "running", ValueKind = MetricValueKind.Boolean, Min = null, Max = null, States = null },
            new MetricInfo
            {
                Key = "mode", ValueKind = MetricValueKind.StateSet, Min = null, Max = null, States = ["auto", "off"],
            },
        ],
        StatusCodes = ["S001", "S002"],
    };

    public static ScenarioSample Sample(
        IReadOnlyDictionary<string, object> values,
        params string[] codes
    ) =>
        new() { Values = values, StatusCodes = codes, IsRunning = true };
}
