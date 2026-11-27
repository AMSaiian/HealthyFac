namespace HealthyFac.Telemetry.Web.DefinitionDrafts;

/// <summary>
/// Defines route constants for the definition draft endpoints.
/// </summary>
public static class DefinitionDraftRoutes
{
    public const string IdParameter = "id";
    public const string MachineIdParameter = "machineId";
    public const string AttemptIdParameter = "attemptId";
    public const string ChatIdParameter = "chatId";
    public const string DraftBaseRoute = "/definition-drafts";
    public const string MachineDraftsRoute = "/machines/{" + MachineIdParameter + "}/definition-drafts";
    public const string AttemptDraftsRoute = "/ingest-attempts/{attemptId}/definition-drafts";
    public const string ChatDraftsRoute = "/chats/{" + ChatIdParameter + "}/definition-drafts";
    public const string Root = "/";
    public const string Search = "/search";
    public const string PendingCounts = "/pending-counts";
    public const string Dismiss = "/{" + IdParameter + "}/dismiss";
    public const string DismissAll = "/dismiss";
    public const string ApplyPhase = "/operating-phases/{" + IdParameter + "}/apply";
}
