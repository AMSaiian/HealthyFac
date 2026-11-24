using HealthyFac.Core.Application.WorkOrders.ApplySchedule;
using HealthyFac.Core.IntegrationTests.Shifts.Data;

namespace HealthyFac.Core.IntegrationTests.WorkOrders.Data;

public static class ApplyScheduleTestData
{
    public const string FirstTime = "09:00";
    public const string AfternoonTime = "14:00";
    public const string SecondAfternoonTime = "15:00";
    public const string OverlappingAfternoonTime = "14:30";
    public const string AfterShiftTime = "18:00";
    public const string BeforeNowTime = "05:00";
    public const string LateNowTime = "10:00";
    public const int TwoHours = 120;
    public const string OtherWorkerEmail = "second.worker@example.com";
    public const string InactiveWorkerEmail = "inactive.worker@example.com";
    public const string Changes = "changes";
    public const uint StaleRowVersion = 1;

    public static DateTime PastStart => ShiftTestData.LocalStart(ShiftTestData.Today, BeforeNowTime);

    public static DateTime Start(string time) => ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time);

    public static ScheduleChange Change(
        Guid workOrderId,
        uint rowVersion,
        Guid? assigneeId,
        string? time,
        int estimateMinutes = WorkOrderTestData.OneHour
    ) =>
        new()
        {
            WorkOrderId = workOrderId,
            RowVersion = rowVersion,
            AssigneeId = assigneeId,
            PlannedStartLocal = time is null ? null : Start(time),
            EstimateMinutes = estimateMinutes,
        };

    public static readonly IReadOnlyList<(Guid WorkOrderId, ScheduleChangeRejection Reason)> NoFailures = [];
}
