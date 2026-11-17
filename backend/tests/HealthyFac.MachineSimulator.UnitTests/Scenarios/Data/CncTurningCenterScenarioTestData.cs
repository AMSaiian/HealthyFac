namespace HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;

public static class CncTurningCenterScenarioTestData
{
    /// <summary>The share that covers the 0.5 % noise and the rounding of a drain progress.</summary>
    public const double Tolerance = 0.006;

    public const string CuttingCode = "S002";
    public const string PartChangeCode = "S001";

    /// <summary>The drain progress and the spindle temperature of the curve at it.</summary>
    public static TheoryData<string, double, double> SpindleTemperatures =>
        new()
        {
            { "NormalLimit", 0.5, 60 },
            { "Alarm", 0.8, 65 },
            { "NearTrip", 0.999, 70 },
        };

    /// <summary>The drain progress, a code, and whether the spindle sends it.</summary>
    public static TheoryData<string, double, string, bool> SpindleCodes =>
        new()
        {
            { "WarningBeforeStart", 0.29, "A112", false },
            { "WarningAtStart", 0.3, "A112", true },
            { "VibrationAtAlarmIsNotAboveLimit", 0.8, "A110", false },
            { "VibrationAboveLimit", 0.9, "A110", true },
        };
}
