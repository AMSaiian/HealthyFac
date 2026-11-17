namespace HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;

public static class GenericScenarioTestData
{
    public const int MessageCount = 100;

    /// <summary>A constant random value and the edge that the walk reaches in the range 10 to 20.</summary>
    public static TheoryData<string, double, double> Pushes =>
        new()
        {
            { "AlwaysUp", 1.0, 20 },
            { "AlwaysDown", 0.0, 10 },
        };

    /// <summary>The bounds and the edges of the range that they give.</summary>
    public static TheoryData<string, double?, double?, double, double> Ranges =>
        new()
        {
            { "BothBounds", 10, 20, 10, 20 },
            { "OnlyMaxPositive", null, 100, 0, 100 },
            { "OnlyMaxNegative", null, -5, -10, -5 },
            { "OnlyMinPositive", 10, null, 10, 20 },
            { "OnlyMinZeroIsFixed", 0, null, 0, 0 },
            { "NoBound", null, null, 0, 0 },
        };
}
