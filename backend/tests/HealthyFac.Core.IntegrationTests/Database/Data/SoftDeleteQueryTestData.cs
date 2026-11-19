namespace HealthyFac.Core.IntegrationTests.Database.Data;

public static class SoftDeleteQueryTestData
{
    public static readonly DateTimeOffset FirstDeleteAt = new(
        year: 2026,
        month: 11,
        day: 19,
        hour: 10,
        minute: 0,
        second: 0,
        TimeSpan.Zero);

    public static readonly DateTimeOffset SecondDeleteAt = FirstDeleteAt.AddHours(hours: 1);

    public const int EarlierStateDays = -1;
}
