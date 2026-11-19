namespace HealthyFac.Core.IntegrationTests.Machines.Data;

public static class MachineGoLiveTestData
{
    public const string ExpectedGoLiveOnKey = "expectedGoLiveOn";

    public static readonly DateTimeOffset CreatedNow = new(
        year: 2026, month: 3, day: 10, hour: 23, minute: 30, second: 0, TimeSpan.Zero);

    public static readonly DateTimeOffset LaterNow = new(
        year: 2026, month: 3, day: 20, hour: 10, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly DateOnly DayBeforeToday = new(year: 2026, month: 3, day: 10);
    public static readonly DateOnly TodayInCompanyZone = new(year: 2026, month: 3, day: 11);

    public static readonly DateTime TodayInCompanyZoneStartsAt = new(
        year: 2026, month: 3, day: 10, hour: 22, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateOnly DayAfterToday = new(year: 2026, month: 3, day: 12);

    public static readonly DateTime DayAfterTodayStartsAt = new(
        year: 2026, month: 3, day: 11, hour: 22, minute: 0, second: 0, DateTimeKind.Utc);
}
