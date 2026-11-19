namespace HealthyFac.Core.ApiTests.WorkOrders.Data;

public static class WorkOrderDraftApiTestData
{
    public const string BasePath = "v1/work-order-drafts";
    public const string SearchPath = BasePath + "/search";
    public const string DraftsRead = "drafts:read";
    public const string DraftsWrite = "drafts:write";
    public const string WorkOrdersWrite = "work-orders:write";
    public const string EmptyObject = "{}";
    public const uint StaleRowVersion = uint.MaxValue;

    public static TheoryData<string, string> ApplyWithMissingPermission => new()
    {
        { "Only the drafts permission", DraftsWrite },
        { "Only the work order permission", WorkOrdersWrite },
    };
}
