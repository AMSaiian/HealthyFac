using HealthyFac.Insights.Web.Authorization;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Insights.ApiTests.Realtime.Data;

public static class RealtimeAudiencePermissionTestData
{
    public static TheoryData<string, string, string> Audiences =>
        new()
        {
            {
                "MaintenanceCaseOpened",
                RealtimeAudiences.KindPermissions[RealtimeNotificationKind.MaintenanceCaseOpened],
                InsightsPermissions.Cases.Read
            },
            { "Cases topic", RealtimeAudiences.TopicPermissions[RealtimeTopic.Cases], InsightsPermissions.Cases.Read },
        };
}
