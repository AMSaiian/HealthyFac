namespace HealthyFac.Core.ApiTests.WorkOrders.Data;

public static class WorkOrderApiTestData
{
    public const string BasePath = "v1/work-orders";
    public const string SearchBatchPath = BasePath + "/search/batch";
    public const string SearchPath = "v1/work-orders/search";
    public const string ReadPermission = "work-orders:read";
    public const string WritePermission = "work-orders:write";
    public const string ManagePermission = "work-orders:manage";
    public const string ShiftsManagePermission = "shifts:manage";
    public const string Title = "Replace the filter";
    public const string OtherEmail = "other.orders@healthyfac.test";
    public const string WorkerEmail = "worker.orders@healthyfac.test";
    public const string SecondWorkerEmail = "second.orders@healthyfac.test";
    public const string MalformedId = "not-an-id";
    public const string UnknownPriority = "urgent";
    public const string BadChecklist = "<ol><li data-list=\"x\">a</li></ol>";
    public const string StartTime = "09:00:00";
    public const int TitleTooLongLength = 201;
    public const int OffGridEstimate = 7;
    public const int ValidEstimate = 60;
    public const int TooManyFiles = 21;
    public const int FirstFutureDaysAhead = 3;
    public const string Morning = "08:00-12:00";
    public const string Afternoon = "13:00-17:00";
    public const string SearchBody = "{\"offset\":0,\"limit\":50,\"filter\":{}}";

    public static TheoryData<string, string> InvalidHistoryLimits => new()
    {
        { "LimitZero", "?limit=0" },
        { "LimitAboveMaximum", "?limit=101" },
        { "LimitMissing", string.Empty },
    };

    public const string ApplyPath = BasePath + "/schedule/apply";
    public const int OverApplyLimit = 101;
    public const string PastStart = "2020-01-06T09:00:00";

    private const string ApplyChangeStart = "{\"workOrderId\":\"{id}\",\"rowVersion\":1,";
    private const string ApplyEstimate = "\"estimateMinutes\":60}";

    public static TheoryData<string, string> InvalidApplyBodies => new()
    {
        { "NoChanges", "{\"changes\":[]}" },
        {
            "AssigneeWithoutStart",
            "{\"changes\":[" + ApplyChangeStart + "\"assigneeId\":\"{id}\"," + ApplyEstimate + "]}"
        },
        {
            "StartWithoutAssignee",
            "{\"changes\":["
            + ApplyChangeStart
            + "\"plannedStartLocal\":\"2030-01-01T09:00:00\","
            + ApplyEstimate
            + "]}"
        },
        {
            "RepeatedWorkOrder",
            "{\"changes\":[" + ApplyChangeStart + ApplyEstimate + "," + ApplyChangeStart + ApplyEstimate + "]}"
        },
    };

    public static TheoryData<string, string, string> WorkerWriteRoutes => new()
    {
        { "Assign", "PUT", BasePath + "/{id}/assignee" },
        { "Move", "POST", BasePath + "/{id}/move" },
        { "ApplySchedule", "POST", BasePath + "/schedule/apply" },
        { "Unassign", "DELETE", BasePath + "/{id}/assignee?rowVersion=1" },
        { "Delete", "DELETE", BasePath + "/{id}?rowVersion=1" },
    };
}
