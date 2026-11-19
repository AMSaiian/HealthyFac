namespace HealthyFac.Core.Web.WorkOrders.Drafts;

/// <summary>
/// Defines route constants for the work order draft endpoints.
/// </summary>
public static class WorkOrderDraftRoutes
{
    public const string BaseRoute = "/work-order-drafts";
    public const string IdParameter = "id";
    public const string Search = "/search";
    public const string ById = "/{" + IdParameter + "}";
    public const string Apply = ById + "/apply";
    public const string Dismiss = ById + "/dismiss";
}
