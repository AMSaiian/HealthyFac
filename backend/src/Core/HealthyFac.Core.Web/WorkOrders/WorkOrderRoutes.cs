namespace HealthyFac.Core.Web.WorkOrders;

/// <summary>
/// Defines route constants for work order endpoints.
/// </summary>
public static class WorkOrderRoutes
{
    public const string BaseRoute = "/work-orders";
    public const string Root = "/";
    public const string IdParameter = "id";
    public const string RowVersionParameter = "rowVersion";
    public const string ById = "/{" + IdParameter + "}";
    public const string Search = "/search";
    public const string SearchBatch = Search + "/batch";
    public const string Status = ById + "/status";
    public const string History = ById + "/history";
    public const string Assignee = ById + "/assignee";
    public const string Move = ById + "/move";
    public const string ResourceLocationFormat = "{0}/{1}";
}
