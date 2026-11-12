using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.WorkOrders.Data;

public static class WorkOrderTestData
{
    public const string CompanyName = "Work order company";
    public const string EmployeeEmail = "worker@example.com";
    public const string Title = "Replace the filter";
    public const int MissingRevision = 5;
    public const int SecondRevision = 2;
    public const string OtherCompanyName = "Other work order company";
    public const string MachineName = "Lathe 7";
    public const string MachineSerialNumber = "WO-LATHE-7";
    public const string OtherMachineSerialNumber = "WO-LATHE-8";
    public const string DeactivatedEmail = "gone.worker@example.com";
    public const string ScheduledTime = "09:00";
    public const string OverlappingTime = "09:30";
    public const string LunchTime = "12:30";
    public const string NextTime = "10:00";
    public const string OffGridTime = "09:03";
    public const int OneHour = 60;
    public const int FileCount = 2;

    public static CreateWorkOrderRequest NewRequest(
        Guid companyId,
        AccessScope scope,
        Guid? machineId = null,
        Guid? assigneeId = null,
        DateTime? plannedStartLocal = null,
        int estimateMinutes = OneHour
    ) =>
        new()
        {
            CompanyId = companyId,
            Scope = scope,
            Title = Title,
            Priority = WorkOrderPriority.High,
            Category = WorkOrderCategory.Repair,
            MachineId = machineId,
            EstimateMinutes = estimateMinutes,
            FileIds = [Guid.CreateVersion7(), Guid.CreateVersion7()],
            AssigneeId = assigneeId,
            PlannedStartLocal = plannedStartLocal,
        };

    public const string ChangedTitle = "Replace the filter and the seal";
    public const int LongerEstimate = 90;
    public static readonly Guid FileA = Guid.Parse(input: "3c9f4085-ad61-4eb1-a044-5c6d8fab2e34");
    public static readonly Guid FileB = Guid.Parse(input: "4da05196-be72-4fc2-b155-6d7e9abc3f45");
    public static readonly Guid FileC = Guid.Parse(input: "5eb162a7-cf83-40d3-8266-7e8fabcd4056");

    public static CreateWorkOrderRequest WithFiles(CreateWorkOrderRequest request, params Guid[] fileIds) =>
        request with { FileIds = fileIds };

    public static UpdateWorkOrderRequest UpdateRequest(
        Guid companyId,
        AccessScope scope,
        Guid id,
        uint rowVersion,
        IReadOnlyList<Guid> fileIds,
        int estimateMinutes = OneHour,
        string title = ChangedTitle
    ) =>
        new()
        {
            CompanyId = companyId,
            Scope = scope,
            Id = id,
            Title = title,
            Priority = WorkOrderPriority.Critical,
            Category = WorkOrderCategory.Inspection,
            EstimateMinutes = estimateMinutes,
            FileIds = fileIds,
            RowVersion = rowVersion,
        };

    public const string AlphaTitle = "Alpha pump";
    public const string BravoTitle = "Bravo valve";
    public const string CharlieTitle = "Charlie belt";
    public const string DeltaTitle = "Delta fan";
    public const string OwnBacklogTitle = "Echo own";
    public const string SecondMachineSerialNumber = "WO-LATHE-9";
    public const string SecondMachineName = "Lathe 9";
    public const int GridLimit = 50;

    public static SearchWorkOrdersRequest SearchRequest(
        Guid companyId,
        AccessScope scope,
        WorkOrderGridFilter? filter = null,
        GridSort<WorkOrderSortColumn>? sort = null
    ) =>
        new()
        {
            CompanyId = companyId,
            Scope = scope,
            Offset = 0,
            Limit = GridLimit,
            Sort = sort,
            Filter = filter ?? new WorkOrderGridFilter(),
        };

    public static SearchWorkOrderBatchRequest BatchRequest(
        Guid companyId,
        AccessScope scope,
        IReadOnlyList<WorkOrderGridFilter> filters
    ) =>
        new()
        {
            CompanyId = companyId,
            Scope = scope,
            Searches =
            [
                .. filters.Select(filter => new SearchWorkOrderBatchEntry
                {
                    Offset = 0,
                    Limit = GridLimit,
                    Filter = filter,
                }),
            ],
        };

    public static TheoryData<int> InvalidBatchSizes { get; } =
        new() { 0, WorkOrderValidationConstants.BatchMaxSearches + 1 };

    public static TheoryData<string, int, string> StartsOutsideTheRules => new()
    {
        { "OffGridTime", 1, OffGridTime },
        { "InThePast", 0, "06:00" },
        { "BeyondOneYear", ShiftValidationConstants.MaxDaysAhead + 1, ScheduledTime },
    };

    public static TheoryData<string, Func<ShiftTenant, Guid?>> OwnScopeWithStartCases => new()
    {
        { "WithAssignee", tenant => tenant.EmployeeId },
        { "WithoutAssignee", _ => null },
    };

    public static GridSort<WorkOrderSortColumn> SortBy(WorkOrderSortColumn column, SortDirection direction) =>
        new() { Column = column, Direction = direction };

    public static CreateWorkOrderRequest Described(
        CreateWorkOrderRequest request,
        string title,
        WorkOrderPriority priority,
        WorkOrderCategory category
    ) =>
        request with { Title = title, Priority = priority, Category = category };
}
