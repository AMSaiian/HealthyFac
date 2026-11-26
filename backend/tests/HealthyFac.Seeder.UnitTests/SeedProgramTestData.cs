namespace HealthyFac.Seeder.UnitTests;

public static class SeedProgramTestData
{
    public static TheoryData<string[]> KnownArguments =>
    [
        new[] { "--telemetry" },
        new[] { "--reset", "--telemetry" },
    ];
}
