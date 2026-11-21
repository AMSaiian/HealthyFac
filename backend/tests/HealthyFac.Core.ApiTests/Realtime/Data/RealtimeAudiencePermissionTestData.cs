using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Core.ApiTests.Realtime.Data;

public static class RealtimeAudiencePermissionTestData
{
    public static TheoryData<string, string, string> Audiences =>
        new()
        {
            {
                "WorkOrderBlocked", RealtimeAudiences.KindPermissions[RealtimeNotificationKind.WorkOrderBlocked],
                CorePermissions.WorkOrders.Manage
            },
            {
                "WorkOrderDraftCreated",
                RealtimeAudiences.KindPermissions[RealtimeNotificationKind.WorkOrderDraftCreated],
                CorePermissions.Drafts.Read
            },
            {
                "ProvisioningFailed", RealtimeAudiences.KindPermissions[RealtimeNotificationKind.ProvisioningFailed],
                CorePermissions.Employees.Manage
            },
            {
                "WorkOrders topic", RealtimeAudiences.TopicPermissions[RealtimeTopic.WorkOrders],
                CorePermissions.WorkOrders.Read
            },
            {
                "Schedule topic", RealtimeAudiences.TopicPermissions[RealtimeTopic.Schedule],
                CorePermissions.Shifts.Read
            },
            {
                "Machines topic", RealtimeAudiences.TopicPermissions[RealtimeTopic.Machines],
                CorePermissions.Machines.Read
            },
        };
}
