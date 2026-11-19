namespace HealthyFac.Telemetry.IntegrationTests.SignalEpisodes.Data;

public static class SignalReadTestData
{
    public const int HistoryBucketCap = 500;

    public const int HourSeconds = 3600;

    public const int HourBucketCount = 120;

    public const int HourBucketSeconds = 30;

    public static TheoryData<string, int, int> InvalidHistoryRanges =>
        new()
        {
            { "bucketCountZero", 0, HourSeconds },
            { "bucketCountAboveCap", HistoryBucketCap + 1, HourSeconds },
            { "rangeEndEqualsStart", HourBucketCount, 0 },
            { "rangeEndBeforeStart", HourBucketCount, -HourSeconds },
        };
}
