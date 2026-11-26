using HealthyFac.Telemetry.ApiTests.Realtime.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.ApiTests.Realtime;

[Feature(TestFeatures.Realtime)]
public sealed class RealtimeAudiencePermissionTests
{
    [Theory]
    [MemberData(
        nameof(RealtimeAudiencePermissionTestData.Audiences), MemberType = typeof(RealtimeAudiencePermissionTestData))]
    public void RealtimeAudiences_ForTheAudience_UseThePermissionOfTheService(
        string audience,
        string audiencePermission,
        string servicePermission
    )
    {
        audiencePermission.Should().Be(servicePermission, audience);
    }
}
