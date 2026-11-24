using HealthyFac.MachineSimulator.Scenarios.Modules;

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

    /// <summary>
    /// The drain at progress 0.99: the module, the key, the seconds of the run, the curve value at the progress, and
    /// whether the curve value is the upper bound (a rising curve) or the lower bound (a falling curve).
    /// </summary>
    public static TheoryData<string, ModuleName, string, int, double, bool> ClampedValues =>
        new()
        {
            { "PartChangeSpindleTemperature", ModuleName.Spindle, "spindle.temperature", 45, 69.75, true },
            { "CuttingSpindleTemperature", ModuleName.Spindle, "spindle.temperature", 10, 69.75, true },
            { "PartChangeHydraulicPressure", ModuleName.Hydraulics, "hydraulic.pressure", 45, 25.15, false },
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
