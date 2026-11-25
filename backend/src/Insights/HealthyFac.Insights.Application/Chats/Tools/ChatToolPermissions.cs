namespace HealthyFac.Insights.Application.Chats.Tools;

/// <summary>
/// Defines the permissions that the token must hold for a chat tool. Each service owns its permission names, and the
/// token exchange adds them to the token of the user.
/// </summary>
public static class ChatToolPermissions
{
    public static class Core
    {
        public const string MachinesRead = "machines:read";
        public const string WorkOrdersRead = "work-orders:read";
        public const string EmployeesRead = "employees:read";
        public const string ShiftsRead = "shifts:read";
    }

    public static class Telemetry
    {
        public const string SignalsRead = "signals:read";
        public const string MetricDefinitionsRead = "metric-definitions:read";
        public const string StatusDefinitionsRead = "status-definitions:read";
    }

    public static class Insights
    {
        public const string CasesRead = "cases:read";
        public const string IngestRead = "ingest:read";
    }

    public static class Document
    {
        public const string FilesRead = "files:read";
    }
}
