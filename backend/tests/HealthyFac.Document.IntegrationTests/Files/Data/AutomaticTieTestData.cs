namespace HealthyFac.Document.IntegrationTests.Files.Data;

public static class AutomaticTieTestData
{
    public static readonly DateTime QueuedAt =
        new(year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static TheoryData<AutomaticTieSource> Sources =>
        new()
        {
            AutomaticTieSource.DraftApplied,
            AutomaticTieSource.MachineMatched,
        };
}
