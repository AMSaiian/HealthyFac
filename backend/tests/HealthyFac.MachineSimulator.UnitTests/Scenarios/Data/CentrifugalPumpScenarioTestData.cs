namespace HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;

public static class CentrifugalPumpScenarioTestData
{
    public const double Tolerance = 0.006;

    /// <summary>The drain progress of the bearings and the DE bearing temperature of the curve at it.</summary>
    public static TheoryData<string, double, double> BearingTemperatures =>
        new()
        {
            { "NormalLimit", 0.5, 80 },
            { "Alarm", 0.8, 90 },
            { "NearTrip", 0.999, 100 },
        };

    /// <summary>The drain progress of the bearings, a code, and whether the pump sends it.</summary>
    public static TheoryData<string, double, string, bool> BearingCodes =>
        new()
        {
            { "TemperatureAtLimitIsNotAbove", 0.8, "P-04", false },
            { "TemperatureAboveLimit", 0.9, "P-04", true },
            { "VibrationAtLimitIsNotAbove", 0.8, "P-06", false },
            { "VibrationAboveLimit", 0.9, "P-06", true },
        };
}
