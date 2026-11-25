using HealthyFac.Core.Domain.WorkOrders;

namespace HealthyFac.Seeder.IntegrationTests.WorkOrders.Data;

public static class WorkOrdersTestData
{
    public const int OrderCount = 13;
    public const string PumpSealTitle = "Replace the mechanical seal of the pump";
    public const string PumpCouplingTitle = "Realign the coupling of the pump and the motor";
    public const string PumpCouplingCompletedAt = "wd-1 16:45";
    public static readonly TimeSpan MomentTolerance = TimeSpan.FromSeconds(seconds: 1);
    public const string PumpSealKey = "pump-seal";
    public const string CoolantCheckTitle = "Check the coolant concentration and top up";
    public const string BacklogStatusItem = "work-orders.json#wear-rings.status.0.inProgress";
    public const string NoAssignmentRule = "has no assignment";
    public const string OutsideShiftMessage = "The slot is not inside one work block of the employee.";
    public const string SlotTakenMessage = "The slot overlaps another work order of the employee.";
    public const string DraftNotPendingMessage = "draftNotPending";

    /// <summary>A Wednesday 11:00 in Kyiv (UTC+3 in June).</summary>
    public static readonly DateTimeOffset Anchor = new(
        year: 2027, month: 6, day: 2, hour: 8, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly IReadOnlyList<string> PumpSealMoments =
        ["wd-6 15:00", "wd-6 15:05", "wd-4 08:05", "wd-4 11:00"];

    public static readonly IReadOnlyList<string> PumpSealActors = ["nadiia", "nadiia", "andrii", "andrii"];

    public static readonly IReadOnlyList<WorkOrderStatus> RepeatedStatuses =
    [
        WorkOrderStatus.Backlog,
        WorkOrderStatus.Staged,
        WorkOrderStatus.InProgress,
        WorkOrderStatus.Blocked,
        WorkOrderStatus.InProgress,
    ];

    public static readonly IReadOnlyList<string> RepeatedActors = ["nadiia", "nadiia", "sofiia", "sofiia", "nadiia"];

    public static readonly IReadOnlyList<WorkOrderStatus> PumpSealStatuses =
        [WorkOrderStatus.Backlog, WorkOrderStatus.Staged, WorkOrderStatus.InProgress, WorkOrderStatus.Completed];

    public static readonly IReadOnlyDictionary<WorkOrderStatus, int> StatusCounts =
        new Dictionary<WorkOrderStatus, int>
        {
            [WorkOrderStatus.Completed] = 4,
            [WorkOrderStatus.Rejected] = 1,
            [WorkOrderStatus.Blocked] = 1,
            [WorkOrderStatus.Staged] = 5,
            [WorkOrderStatus.Backlog] = 2,
        };
}
