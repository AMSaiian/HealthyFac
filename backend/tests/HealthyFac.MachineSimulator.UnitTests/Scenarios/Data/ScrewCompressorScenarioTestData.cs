namespace HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;

public static class ScrewCompressorScenarioTestData
{
    public const double Tolerance = 0.006;

    /// <summary>The drain progress of the oil separator and the pressure drop of the curve at it.</summary>
    public static TheoryData<string, double, double> SeparatorDrops =>
        new()
        {
            { "NormalLimit", 0.5, 0.6 },
            { "Alarm", 0.8, 0.8 },
            { "NearTrip", 0.999, 1.0 },
        };

    /// <summary>The drain progress of the air filter and the pressure drop in kPa of the curve at it.</summary>
    public static TheoryData<string, double, double> AirFilterDrops =>
        new()
        {
            { "NormalLimit", 0.5, 3.0 },
            { "Alarm", 0.8, 5.0 },
            { "NearTrip", 0.999, 6.0 },
        };

    /// <summary>The drain progress of the air filter, the code W3060, and whether it is sent.</summary>
    public static TheoryData<string, double, bool> AirFilterCodes =>
        new()
        {
            { "AtLimitIsNotAbove", 0.8, false },
            { "AboveLimit", 0.9, true },
        };

    /// <summary>The drain progress of the oil separator, the code W3050, and whether it is sent.</summary>
    public static TheoryData<string, double, bool> SeparatorCodes =>
        new()
        {
            { "AtLimitIsNotAbove", 0.8, false },
            { "AboveLimit", 0.9, true },
        };
}
