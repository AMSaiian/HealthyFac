using HealthyFac.MachineSimulator.Scenarios.Curves;

namespace HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;

public static class DrainCurveTestData
{
    public static readonly DrainCurve SpindleTemperature = DrainCurve.FourPoints(
        reference: 40, normalLimit: 60, alarm: 65, trip: 70);

    public static TheoryData<string, double, double> SpindleTemperatureValues =>
        new()
        {
            { "Reference", 0, 40 },
            { "NormalLimit", 0.5, 60 },
            { "Alarm", 0.8, 65 },
            { "Trip", 1.0, 70 },
            { "BetweenReferenceAndNormalLimit", 0.25, 50 },
            { "AboveOne", 1.5, 70 },
        };

    public static TheoryData<string, double, double> TwoPointValues =>
        new()
        {
            { "Start", 0, 31.2 },
            { "Middle", 0.5, 32.3 },
            { "End", 1.0, 33.4 },
        };
}
