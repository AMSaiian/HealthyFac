namespace HealthyFac.Core.Web.Drafts;

/// <summary>
/// Defines route constants for the draft endpoints.
/// </summary>
public static class DraftRoutes
{
    public const string MachineDraftBaseRoute = "/machine-drafts";
    public const string DraftBaseRoute = "/drafts";
    public const string IdParameter = "id";
    public const string MachineIdParameter = "machineId";
    public const string AttemptIdParameter = "attemptId";
    public const string MachineDraftsOfMachineRoute = "/machines/{" + MachineIdParameter + "}/drafts";
    public const string AttemptDraftsRoute = "/ingest-attempts/{" + AttemptIdParameter + "}/drafts";
    public const string Root = "/";
    public const string Search = "/search";
    public const string ById = "/{" + IdParameter + "}";
    public const string Apply = ById + "/apply";
    public const string Dismiss = ById + "/dismiss";
    public const string MarkApplied = "/mark-applied";
    public const string DismissAll = "/dismiss";
}
