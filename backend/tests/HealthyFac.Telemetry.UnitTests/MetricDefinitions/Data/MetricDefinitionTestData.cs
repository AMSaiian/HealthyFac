using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricUnits;

namespace HealthyFac.Telemetry.UnitTests.MetricDefinitions.Data;

public static class MetricDefinitionTestData
{
    private const string Name = "Spindle temperature";
    private const string Key = "spindle.temperature";

    public static readonly Guid RegistrationId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a01");
    private static readonly QuantityKind TemperatureKind = new() { Name = "Temperature" };
    private static readonly QuantityKind DiscreteKind = new() { Name = DiscreteUnits.KindName };

    public static readonly MeasurementUnit CelsiusUnit = new(
        TemperatureKind, name: "DegreeCelsius", abbreviation: "°C");

    public static readonly MeasurementUnit BooleanUnit =
        new(DiscreteKind, DiscreteUnits.BooleanName, string.Empty);

    public static readonly MeasurementUnit StateUnit =
        new(DiscreteKind, DiscreteUnits.StateName, string.Empty);

    public static IReadOnlyList<string> Modes { get; } = ["auto", "manual", "off"];

    public static MetricDefinition CreateNumeric(double? min = null, double? max = null) =>
        new(RegistrationId, Key, Name, description: null, MetricValueKind.Numeric, CelsiusUnit, min, max, states: null);

    public static MetricDefinition CreateStateSet() =>
        new(
            RegistrationId, key: "spindle.mode", Name, description: null, MetricValueKind.StateSet,
            StateUnit, min: null, max: null, Modes);

    public static MetricDefinition CreateBoolean() =>
        new(
            RegistrationId, key: "door.open", name: "Door open", description: null, MetricValueKind.Boolean,
            BooleanUnit, min: null, max: null, states: null);

    public static MetricDefinition CreateWithKey(string key) =>
        new(
            RegistrationId, key, Name, description: null, MetricValueKind.Boolean, BooleanUnit, min: null,
            max: null, states: null);

    public static MetricDefinition CreateWithFields(
        MetricValueKind kind,
        MeasurementUnit unit,
        double? min,
        double? max,
        string[]? states
    ) =>
        new(RegistrationId, key: "spindle.mode", Name, description: null, kind, unit, min, max, states);

    public static TheoryData<string> ValidKeys =>
    [
        "a",
        "spindle.temperature",
        "door_open",
        "line1.zone2.value_3",
        new string(c: 'a', MetricDefinitionRules.KeyMaxLength),
    ];

    public static TheoryData<string> InvalidKeys =>
    [
        string.Empty,
        "Spindle",
        "1spindle",
        "spindle temperature",
        "spindle..temperature",
        ".spindle",
        "spindle.",
        "_spindle",
        "spindle-temperature",
        new string(c: 'a', MetricDefinitionRules.KeyMaxLength + 1),
    ];

    public static TheoryData<double?, double?> AscendingRanges => new()
    {
        { -20, null },
        { -20, 200 },
        { 0, double.Epsilon },
        { double.MinValue, double.MaxValue },
    };

    public static TheoryData<double, double> NotAscendingRanges => new()
    {
        { 10, 10 },
        { 10, 5 },
        { 0, -double.Epsilon },
    };

    public static TheoryData<MetricValueKind, MeasurementUnit, double?, double?, string[]?> InvalidCombinations => new()
    {
        { MetricValueKind.Numeric, CelsiusUnit, null, null, new[] { "auto", "off" } },
        { MetricValueKind.Boolean, BooleanUnit, 1, null, null },
        { MetricValueKind.Boolean, BooleanUnit, null, 1, null },
        { MetricValueKind.Boolean, BooleanUnit, null, null, new[] { "auto", "off" } },
        { MetricValueKind.StateSet, StateUnit, 1, null, new[] { "auto", "off" } },
        { MetricValueKind.StateSet, StateUnit, null, null, null },
        { MetricValueKind.StateSet, StateUnit, null, null, new[] { "auto" } },
        { MetricValueKind.StateSet, StateUnit, null, null, Array.Empty<string>() },
        { MetricValueKind.StateSet, StateUnit, null, null, new[] { "auto", "auto" } },
        { MetricValueKind.StateSet, StateUnit, null, null, new[] { "auto", "Off" } },
        { MetricValueKind.StateSet, StateUnit, null, null, new[] { "auto", "o ff" } },
    };

    public static TheoryData<MetricValueKind, MeasurementUnit> DiscreteKindsWithWrongUnit => new()
    {
        { MetricValueKind.Boolean, CelsiusUnit },
        { MetricValueKind.Boolean, StateUnit },
        { MetricValueKind.StateSet, CelsiusUnit },
        { MetricValueKind.StateSet, BooleanUnit },
    };

    public static TheoryData<MeasurementUnit> DiscreteUnitList => [BooleanUnit, StateUnit];
}
