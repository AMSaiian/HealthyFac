using HealthyFac.Core.ApiTests.Realtime.Data;

namespace HealthyFac.Core.ApiTests.Realtime;

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
