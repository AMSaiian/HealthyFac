namespace HealthyFac.Telemetry.IntegrationTests.MetricUnits.Data;

public static class MetricUnitTestData
{
    public const int ParallelRunCount = 2;
    public const string CelsiusAbbreviation = "°C";
    public const string CelsiusUnitName = "DegreeCelsius";
    public const string WrongAbbreviation = "wrong";

    /// <summary>The initial catalog of the spec as <c>Kind.Unit</c> pairs.</summary>
    public static readonly IReadOnlyList<string> ExpectedPairs =
    [
        "Temperature.DegreeCelsius",
        "Temperature.DegreeFahrenheit",
        "Temperature.Kelvin",
        "Pressure.Bar",
        "Pressure.Kilopascal",
        "Pressure.Megapascal",
        "Pressure.PoundForcePerSquareInch",
        "RotationalSpeed.RevolutionPerMinute",
        "RotationalSpeed.RadianPerSecond",
        "Speed.MillimeterPerSecond",
        "Speed.MeterPerMinute",
        "Acceleration.MeterPerSecondSquared",
        "Acceleration.StandardGravity",
        "Length.Millimeter",
        "Length.Micrometer",
        "Frequency.Hertz",
        "ElectricCurrent.Ampere",
        "ElectricPotential.Volt",
        "Power.Watt",
        "Power.Kilowatt",
        "Energy.KilowattHour",
        "VolumeFlow.LiterPerMinute",
        "VolumeFlow.CubicMeterPerHour",
        "Volume.Liter",
        "Ratio.Percent",
        "RelativeHumidity.Percent",
        "Duration.Second",
        "Duration.Hour",
        "Scalar.Amount",
        "Discrete.Boolean",
        "Discrete.State",
    ];
}
