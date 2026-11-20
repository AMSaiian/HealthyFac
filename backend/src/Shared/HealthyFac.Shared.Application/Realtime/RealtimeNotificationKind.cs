using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.Application.Realtime;

/// <summary>
/// Names what a real-time notification tells the user. The value travels over the hub, so treat it as a contract.
/// A user-targeted kind reaches one user. A group kind reaches the users who hold its permission
/// (<see cref="RealtimeAudiences.KindPermissions"/>).
/// </summary>
public sealed class RealtimeNotificationKind : Enumeration<RealtimeNotificationKind>
{
    public static readonly RealtimeNotificationKind ShiftChanged = new(value: "shiftChanged");
    public static readonly RealtimeNotificationKind WorkOrderAssigned = new(value: "workOrderAssigned");
    public static readonly RealtimeNotificationKind WorkOrderUnassigned = new(value: "workOrderUnassigned");
    public static readonly RealtimeNotificationKind WorkOrderEstimateChanged = new(value: "workOrderEstimateChanged");
    public static readonly RealtimeNotificationKind WorkOrderRescheduled = new(value: "workOrderRescheduled");

    public static readonly RealtimeNotificationKind WorkOrderReturnedToBacklog =
        new(value: "workOrderReturnedToBacklog");

    public static readonly RealtimeNotificationKind IngestFinished = new(value: "ingestFinished");
    public static readonly RealtimeNotificationKind WorkOrderBlocked = new(value: "workOrderBlocked");
    public static readonly RealtimeNotificationKind WorkOrderDraftCreated = new(value: "workOrderDraftCreated");
    public static readonly RealtimeNotificationKind ProvisioningFailed = new(value: "provisioningFailed");
    public static readonly RealtimeNotificationKind MaintenanceCaseOpened = new(value: "maintenanceCaseOpened");

    private RealtimeNotificationKind(string value)
        : base(value)
    {
    }
}
