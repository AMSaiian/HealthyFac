using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Telemetry.Web.Authorization;

namespace HealthyFac.Telemetry.ApiTests.Realtime.Data;

public static class RealtimeAudiencePermissionTestData
{
    public static TheoryData<string, string, string> Audiences =>
        new()
        {
            {
                "DefinitionDrafts topic", RealtimeAudiences.TopicPermissions[RealtimeTopic.DefinitionDrafts],
                TelemetryPermissions.DefinitionDrafts.Read
            },
        };
}
