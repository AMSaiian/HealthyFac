namespace HealthyFac.Seeder.UnitTests.Moments.Data;

public static class RelativeMomentTestData
{
    public static TheoryData<string> InvalidTexts => new()
    {
        "wd+x 09:00",
        "d-1 25:00",
        "d-1 09:60",
        "wd-2",
        "h+2 09:00",
        "x",
        string.Empty,
        "wd+1 9:00",
        "d+99999999999",
        "h-1:60",
        "h-1:5",
        "h-1 :05",
        "d-1:30",
        "wd-1:30 10:00",
        "h-1:30 10:00",
    };

    public static TheoryData<string> ValidTexts => new()
    {
        "wd+0 09:00",
        "wd-12 23:59",
        "d+2 03:30",
        "d-1 00:00",
        "d-30",
        "d+0",
        "h-3",
        "h+48",
        "h-22:05",
        "h+0:30",
    };
}
