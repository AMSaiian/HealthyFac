using System.Globalization;
using HealthyFac.Telemetry.Domain.MetricUnits;
using UnitsNet;
using UnitsNet.Units;

namespace HealthyFac.Telemetry.Infrastructure.MetricUnits.Persistence;

/// <summary>
/// Lists the units of the catalog as typed UnitsNet enum members. The seed reads the quantity name, the unit name,
/// and the abbreviation from UnitsNet, so the names equal the library names. A typo fails at compile time. The
/// seed is safe to run on every start.
/// </summary>
internal static class MetricUnitSeed
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static IReadOnlyList<MetricUnitSeedRow> Rows { get; } =
    [
        .. Of(
            Temperature.Info.Name, TemperatureUnit.DegreeCelsius, TemperatureUnit.DegreeFahrenheit,
            TemperatureUnit.Kelvin),
        .. Of(
            Pressure.Info.Name,
            PressureUnit.Bar,
            PressureUnit.Kilopascal,
            PressureUnit.Megapascal,
            PressureUnit.PoundForcePerSquareInch),
        .. Of(
            RotationalSpeed.Info.Name,
            RotationalSpeedUnit.RevolutionPerMinute,
            RotationalSpeedUnit.RadianPerSecond),
        .. Of(Speed.Info.Name, SpeedUnit.MillimeterPerSecond, SpeedUnit.MeterPerMinute),
        .. Of(
            Acceleration.Info.Name,
            AccelerationUnit.MeterPerSecondSquared,
            AccelerationUnit.StandardGravity),
        .. Of(Length.Info.Name, LengthUnit.Millimeter, LengthUnit.Micrometer),
        .. Of(Frequency.Info.Name, FrequencyUnit.Hertz),
        .. Of(ElectricCurrent.Info.Name, ElectricCurrentUnit.Ampere),
        .. Of(ElectricPotential.Info.Name, ElectricPotentialUnit.Volt),
        .. Of(Power.Info.Name, PowerUnit.Watt, PowerUnit.Kilowatt),
        .. Of(Energy.Info.Name, EnergyUnit.KilowattHour),
        .. Of(VolumeFlow.Info.Name, VolumeFlowUnit.LiterPerMinute, VolumeFlowUnit.CubicMeterPerHour),
        .. Of(Volume.Info.Name, VolumeUnit.Liter),
        .. Of(Ratio.Info.Name, RatioUnit.Percent),
        .. Of(RelativeHumidity.Info.Name, RelativeHumidityUnit.Percent),
        .. Of(Duration.Info.Name, DurationUnit.Second, DurationUnit.Hour),
        .. Of(Scalar.Info.Name, ScalarUnit.Amount),
        .. Discrete(DiscreteUnits.BooleanName, DiscreteUnits.StateName),
    ];

    private static IEnumerable<MetricUnitSeedRow> Discrete(params string[] names) =>
        names.Select(name => new MetricUnitSeedRow
        {
            QuantityName = DiscreteUnits.KindName,
            UnitName = name,
            Abbreviation = string.Empty,
        });

    private static IEnumerable<MetricUnitSeedRow> Of<TUnit>(string quantityName, params TUnit[] units)
        where TUnit : struct, Enum =>
        units.Select(unit => new MetricUnitSeedRow
        {
            QuantityName = quantityName,
            UnitName = unit.ToString(),
            Abbreviation = UnitsNetSetup.Default.UnitAbbreviations.GetDefaultAbbreviation(unit, Culture),
        });
}
