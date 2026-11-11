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
}
