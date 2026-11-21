using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Shared.UnitTests.Realtime.Data;

public static class RealtimeAudiencesTestData
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0f1b4c0e-3c3a-4d57-9a53-3c9b8a1d2e10");

    public static readonly IReadOnlyList<RealtimeNotificationKind> UserTargetedKinds =
    [
        RealtimeNotificationKind.ShiftChanged,
        RealtimeNotificationKind.WorkOrderAssigned,
        RealtimeNotificationKind.WorkOrderUnassigned,
        RealtimeNotificationKind.WorkOrderEstimateChanged,
        RealtimeNotificationKind.WorkOrderRescheduled,
        RealtimeNotificationKind.WorkOrderReturnedToBacklog,
        RealtimeNotificationKind.IngestFinished,
    ];
}
