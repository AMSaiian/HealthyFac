using HealthyFac.Telemetry.Application.MetricSchemas;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricUnits;
using UnitsNet;
using UnitsNet.Units;

namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>
/// The sample metrics of each seed machine: a numeric temperature with a range, a boolean, and a state set. The seeder
/// creates them with the entity constructor and the schema generator, as the apply use case does.
/// </summary>
internal static class SampleMetricSet
{
    public const string TemperatureKey = "spindle.temperature";
    public const string DoorKey = "door.open";
    public const string ModeKey = "spindle.mode";
    public const double TemperatureMin = -20;
    public const double TemperatureMax = 200;

    private static readonly IReadOnlyList<string> Modes = ["auto", "manual", "off"];

    public static string TemperatureQuantityName => Temperature.Info.Name;

    public static string TemperatureUnitName => nameof(TemperatureUnit.DegreeCelsius);

    /// <summary>
    /// Creates the three sample definitions of a machine.
    /// </summary>
    /// <param name="registrationId">The registration of the machine.</param>
    /// <param name="temperatureUnit">The catalog unit of the temperature.</param>
    /// <param name="booleanUnit">The discrete unit of a boolean.</param>
    /// <param name="stateUnit">The discrete unit of a state set.</param>
    /// <returns>The definitions.</returns>
    public static IReadOnlyList<MetricDefinition> CreateDefinitions(
        Guid registrationId,
        MeasurementUnit temperatureUnit,
        MeasurementUnit booleanUnit,
        MeasurementUnit stateUnit
    ) =>
    [
        new(
            registrationId,
            TemperatureKey,
            name: "Spindle temperature",
            description: null,
            MetricValueKind.Numeric,
            temperatureUnit,
            TemperatureMin,
            TemperatureMax,
            states: null),
        new(
            registrationId,
            DoorKey,
            name: "Door open",
            description: null,
            MetricValueKind.Boolean,
            booleanUnit,
            min: null,
            max: null,
            states: null),
        new(
            registrationId,
            ModeKey,
            name: "Spindle mode",
            description: null,
            MetricValueKind.StateSet,
            stateUnit,
            min: null,
            max: null,
            Modes),
    ];

    public static IReadOnlyList<MetricSchema> ToSchemaMetrics(
        IEnumerable<MetricDefinition> definitions,
        IReadOnlyList<MeasurementUnit> units
    ) =>
        definitions
            .Select(definition => new MetricSchema
            {
                Key = definition.Key,
                Name = definition.Name,
                ValueKind = definition.ValueKind,
                UnitQuantity = units.Single(unit => unit.Id == definition.MeasurementUnitId).QuantityKind.Name,
                UnitName = units.Single(unit => unit.Id == definition.MeasurementUnitId).Name,
                Min = definition.Min,
                Max = definition.Max,
                States = definition.States,
            })
            .ToList();
}
