namespace HealthyFac.Core.UnitTests.Machines.Data;

public static class MachineGoLiveRulesTestData
{
    public static TheoryData<string, DateOnly, string, DateTime> StartsOfDay => new()
    {
        {
            "Kyiv in winter",
            new DateOnly(year: 2026, month: 1, day: 15),
            "Europe/Kyiv",
            new DateTime(year: 2026, month: 1, day: 14, hour: 22, minute: 0, second: 0, DateTimeKind.Utc)
        },
        {
            "Kyiv in summer",
            new DateOnly(year: 2026, month: 7, day: 15),
            "Europe/Kyiv",
            new DateTime(year: 2026, month: 7, day: 14, hour: 21, minute: 0, second: 0, DateTimeKind.Utc)
        },
        {
            "Havana on its spring forward day (midnight does not exist)",
            new DateOnly(year: 2026, month: 3, day: 8),
            "America/Havana",
            new DateTime(year: 2026, month: 3, day: 8, hour: 5, minute: 0, second: 0, DateTimeKind.Utc)
        },
    };
}
