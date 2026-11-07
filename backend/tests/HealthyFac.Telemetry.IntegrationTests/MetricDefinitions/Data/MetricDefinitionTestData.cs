using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;

public static class MetricDefinitionTestData
{
    public const int RaceParticipantCount = 2;
    public const string TemperatureKey = "spindle.temperature";
    public const string DoorKey = "door.open";
    public const string ModeKey = "spindle.mode";

    public static IReadOnlyList<string> Modes { get; } = ["auto", "manual", "off"];

    public static CreatedMetricDefinition NewNumeric(string key = TemperatureKey) =>
        new()
        {
            Key = key,
            Name = "Spindle temperature",
            ValueKind = MetricValueKind.Numeric,
            Unit = new MetricUnitReference { QuantityKind = "Temperature", Name = "DegreeCelsius" },
            Min = -20,
            Max = 200,
        };

    public static CreatedMetricDefinition NewBoolean(string key = DoorKey) =>
        new() { Key = key, Name = "Door open", ValueKind = MetricValueKind.Boolean };

    public static CreatedMetricDefinition NewStateSet(string key = ModeKey) =>
        new() { Key = key, Name = "Spindle mode", ValueKind = MetricValueKind.StateSet, States = Modes };

    public static UpdatedMetricDefinition Update(Guid id, string name, double? min = null, double? max = null) =>
        new() { Id = id, Name = name, Min = min, Max = max };

    public static TheoryData<CreatedMetricDefinition, string> InvalidCreated => new()
    {
        { NewNumeric() with { Key = "Bad Key" }, "created[0].key" },
        { NewNumeric() with { Key = string.Empty }, "created[0].key" },
        { NewNumeric() with { Key = new string(c: 'a', MetricDefinitionRules.KeyMaxLength + 1) }, "created[0].key" },
        { NewNumeric() with { Key = "1abc" }, "created[0].key" },
        { NewNumeric() with { Name = string.Empty }, "created[0].name" },
        {
            NewNumeric() with { Name = new string(c: 'n', MetricDefinitionValidationConstants.NameMaxLength + 1) },
            "created[0].name"
        },
        { NewNumeric() with { ValueKind = null }, "created[0].valueKind" },
        { NewNumeric() with { Unit = null }, "created[0].unit" },
        {
            NewBoolean() with { Unit = new MetricUnitReference { QuantityKind = "Temperature", Name = "Kelvin" } },
            "created[0].unit"
        },
        {
            NewNumeric() with { Unit = new MetricUnitReference { QuantityKind = "Discrete", Name = "Boolean" } },
            "created[0].unit"
        },
        { NewNumeric() with { Min = 10, Max = 10 }, "created[0].max" },
        { NewNumeric() with { Min = 10, Max = 5 }, "created[0].max" },
        { NewBoolean() with { Min = 1 }, "created[0].min" },
        { NewBoolean() with { Max = 1 }, "created[0].max" },
        { NewStateSet() with { States = ["auto"] }, "created[0].states" },
        { NewStateSet() with { States = ["auto", "auto"] }, "created[0].states" },
        { NewStateSet() with { States = ["auto", "Off"] }, "created[0].states" },
        { NewStateSet() with { States = null }, "created[0].states" },
        { NewBoolean() with { States = ["a", "b"] }, "created[0].states" },
    };

    public static TheoryData<CreatedMetricDefinition> ValidEdges => new()
    {
        { NewBoolean(new string(c: 'a', MetricDefinitionRules.KeyMaxLength)) },
        { NewNumeric() with { Name = new string(c: 'n', MetricDefinitionValidationConstants.NameMaxLength) } },
        { NewNumeric() with { Min = 9.9, Max = 10 } },
        { NewNumeric() with { Min = null, Max = 10 } },
        { NewNumeric() with { Min = 10, Max = null } },
        { NewStateSet() with { States = ["on", "off"] } },
    };
}
