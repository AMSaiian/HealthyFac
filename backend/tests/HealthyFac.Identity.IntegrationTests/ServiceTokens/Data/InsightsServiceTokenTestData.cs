namespace HealthyFac.Identity.IntegrationTests.ServiceTokens.Data;

public static class InsightsServiceTokenTestData
{
    public const string FilesScope = "insights-files";
    public const string TelemetryScope = "insights-telemetry";

    public static readonly string[] FilesPermissions = ["files:read"];
    public static readonly string[] TelemetryPermissions = ["metric-definitions:read", "status-definitions:read"];
}
