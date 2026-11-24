namespace HealthyFac.Seeder.UnitTests.Moments.Data;

public static class SeedCalendarTestData
{
    public static readonly string KyivZoneId = "Europe/Kyiv";

    public static TheoryData<string, string, DateTimeOffset> UtcCases => new()
    {
        {
            "wd+0 09:00 on a Wednesday", "2027-03-24T10:00:00Z|wd+0 09:00",
            Utc(year: 2027, month: 3, day: 24, hour: 7, minute: 0)
        },
        {
            "wd+0 09:00 on a Saturday", "2027-03-27T10:00:00Z|wd+0 09:00",
            Utc(year: 2027, month: 3, day: 29, hour: 6, minute: 0)
        },
        {
            "wd-1 09:00 on a Monday", "2027-03-22T10:00:00Z|wd-1 09:00",
            Utc(year: 2027, month: 3, day: 19, hour: 7, minute: 0)
        },
        {
            "wd+2 13:00 on a Thursday", "2027-03-25T10:00:00Z|wd+2 13:00",
            Utc(year: 2027, month: 3, day: 29, hour: 10, minute: 0)
        },
        {
            "d-1 10:00 at 00:30 local", "2027-03-24T22:30:00Z|d-1 10:00",
            Utc(year: 2027, month: 3, day: 24, hour: 8, minute: 0)
        },
        { "h-3", "2027-03-25T10:00:00Z|h-3", Utc(year: 2027, month: 3, day: 25, hour: 7, minute: 0) },
        {
            "wd+0 03:30 on a Friday before the clock change", "2027-03-26T10:00:00Z|wd+0 03:30",
            Utc(year: 2027, month: 3, day: 26, hour: 1, minute: 30)
        },
    };

    public static TheoryData<string, string> RejectedCases => new()
    {
        { "a time in the spring clock change", "2027-03-26T10:00:00Z|d+2 03:30" },
        { "a time in the autumn clock change", "2027-10-29T10:00:00Z|d+2 03:30" },
        { "a date without a time as an instant", "2027-03-26T10:00:00Z|d+1" },
    };

    public static DateTimeOffset Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, second: 0, TimeSpan.Zero);
}
