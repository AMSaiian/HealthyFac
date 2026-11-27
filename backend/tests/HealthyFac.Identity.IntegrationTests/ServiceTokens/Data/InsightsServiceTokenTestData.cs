using HealthyFac.Identity.IntegrationTests.Realm.Data;

namespace HealthyFac.Identity.IntegrationTests.ServiceTokens.Data;

public static class InsightsServiceTokenTestData
{
    public const string FilesScope = "insights-files";
    public const string TelemetryScope = "insights-telemetry";
    public const string WorkOrdersScope = "insights-work-orders";

    public static readonly string[] FilesPermissions = ["files:read"];

    public static readonly string[] TelemetryPermissions =
        ["metric-definitions:read", "status-definitions:read", "signals:read"];

    public static readonly string[] WorkOrdersPermissions = ["work-orders:read", "work-orders:read-company"];

    public static TheoryData<string, string, string[], string> Scopes => new()
    {
        { "files", FilesScope, FilesPermissions, RealmTestData.DocumentClientId },
        { "telemetry", TelemetryScope, TelemetryPermissions, RealmTestData.TelemetryClientId },
        { "work orders", WorkOrdersScope, WorkOrdersPermissions, RealmTestData.CoreClientId },
    };
}
