using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.UnitTests.Realtime.Data;

namespace HealthyFac.Shared.UnitTests.Realtime;

public sealed class RealtimeAudiencesTests
{
    [Fact]
    public void KindPermissions_AndUserTargetedKinds_CoverEveryKindExactlyOnce()
    {
        var groupKinds = RealtimeAudiences.KindPermissions.Keys;

        var userTargetedKinds = RealtimeAudiencesTestData.UserTargetedKinds;

        groupKinds.Should().OnlyHaveUniqueItems();
        userTargetedKinds.Should().NotIntersectWith(groupKinds);
        groupKinds.Concat(userTargetedKinds).Should().BeEquivalentTo(RealtimeNotificationKind.All);
    }

    [Fact]
    public void TopicPermissions_ForEveryTopic_HoldsOnePermission()
    {
        var topics = RealtimeAudiences.TopicPermissions.Keys;

        topics.Should().BeEquivalentTo(RealtimeTopic.All);

        RealtimeAudiences.TopicPermissions.Values.Should()
            .OnlyContain(permission => !string.IsNullOrWhiteSpace(permission));
    }

    [Fact]
    public void ForKind_WithKindAndCompany_BuildsTheKindGroupName()
    {
        var group = RealtimeGroups.ForKind(
            RealtimeAudiencesTestData.CompanyId, RealtimeNotificationKind.WorkOrderBlocked);

        group.Should().Be(expected: "company:0f1b4c0e-3c3a-4d57-9a53-3c9b8a1d2e10:kind:workOrderBlocked");
    }

    [Fact]
    public void ForTopic_WithTopicAndCompany_BuildsTheTopicGroupName()
    {
        var group = RealtimeGroups.ForTopic(RealtimeAudiencesTestData.CompanyId, RealtimeTopic.Cases);

        group.Should().Be(expected: "company:0f1b4c0e-3c3a-4d57-9a53-3c9b8a1d2e10:topic:cases");
    }
}
