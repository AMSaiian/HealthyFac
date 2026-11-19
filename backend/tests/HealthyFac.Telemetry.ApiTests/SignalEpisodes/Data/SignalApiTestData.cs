namespace HealthyFac.Telemetry.ApiTests.SignalEpisodes.Data;

public static class SignalApiTestData
{
    public const string MachinesPath = "v1/machines";
    public const string ReadPermission = "signals:read";
    public const string OtherPermission = "metric-definitions:read";
    public const string MetricKey = "spindle.temperature";
    public const string PhaseCode = "cutting";
    public const int RangeSeconds = 3600;
    public const int BucketCount = 120;
    public const int BucketSeconds = 30;

    public static readonly DateTime RangeStart = new(
        year: 2026, month: 11, day: 1, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateTime RangeEnd = RangeStart.AddSeconds(RangeSeconds);

    public static readonly string RangeStartText = RangeStart.ToString(format: "yyyy-MM-ddTHH:mm:ssZ");

    public static readonly string RangeEndText = RangeEnd.ToString(format: "yyyy-MM-ddTHH:mm:ssZ");

    public static readonly (int Second, double Value)[] Readings = [(5, 10), (10, 20), (35, 30)];
}
