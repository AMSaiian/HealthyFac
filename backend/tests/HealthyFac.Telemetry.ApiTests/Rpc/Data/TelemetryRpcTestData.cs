namespace HealthyFac.Telemetry.ApiTests.Rpc.Data;

public static class TelemetryRpcTestData
{
    public const string SignalsRead = "signals:read";
    public const string MetricDefinitionsRead = "metric-definitions:read";
    public const string StatusDefinitionsRead = "status-definitions:read";
    public const string StatusCode = "spindle-stopped";
    public const string StatusSeverity = "error";
    public const string MetricUnit = "°C";
    public const string NumericKind = "numeric";
    public const int RangeSeconds = 3600;

    public static readonly DateTime RangeStart = new(
        year: 2026, month: 11, day: 1, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateTime RangeEnd = RangeStart.AddSeconds(RangeSeconds);

    public static TheoryData<string, string[]> DefinitionPermissionGaps => new()
    {
        { "NoStatusPermission", [MetricDefinitionsRead] },
        { "NoMetricPermission", [StatusDefinitionsRead] },
    };
}
