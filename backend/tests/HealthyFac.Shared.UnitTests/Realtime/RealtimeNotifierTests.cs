using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.UnitTests.Realtime.Data;

namespace HealthyFac.Shared.UnitTests.Realtime;

public sealed class RealtimeNotifierTests
{
    [Theory]
    [MemberData(nameof(RealtimeNotifierTestData.GroupKinds), MemberType = typeof(RealtimeNotifierTestData))]
    public void AddToGroup_WithGroupKind_TargetsTheKindGroupOfTheCompany(
        RealtimeNotificationKind kind,
        string expectedGroup
    )
    {
        var fixture = new NotifierFixture(RealtimeNotifierTestData.ServiceActor);

        fixture.Notifier.AddToGroup(RealtimeNotifierTestData.CompanyId, kind, new { Name = "data" });
        fixture.Notifier.Push();

        var push = fixture.Queue.Items.Should().ContainSingle().Subject.Push;
        push.Target.Should().Be(RealtimePushTarget.Group);
        push.Group.Should().Be(expectedGroup);
        push.Notification!.Kind.Should().Be(kind);
        push.MetricTarget.Should().Be(kind.Value);
    }

    [Fact]
    public void AddHint_WithTopic_TargetsTheTopicGroupOfTheCompany()
    {
        var fixture = new NotifierFixture(RealtimeNotifierTestData.UserActor);

        fixture.Notifier.AddHint(RealtimeNotifierTestData.CompanyId, new RealtimeHint { Topic = RealtimeTopic.Cases });
        fixture.Notifier.Push();

        var push = fixture.Queue.Items.Should().ContainSingle().Subject.Push;
        push.Group.Should().Be(expected: "company:11111111-1111-1111-1111-111111111111:topic:cases");
        push.Hint!.Topic.Should().Be(RealtimeTopic.Cases);
        push.MetricTarget.Should().Be(expected: "cases");
    }

    [Fact]
    public void AddToUser_ForTheActorUser_EnqueuesNothing()
    {
        var fixture = new NotifierFixture(RealtimeNotifierTestData.UserActor);

        fixture.Notifier.AddToUser(
            RealtimeNotifierTestData.ActorUserId,
            RealtimeNotificationKind.ShiftChanged,
            new { Name = "data" });
        fixture.Notifier.Push();

        fixture.Queue.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddToUser_ForAnotherUser_EnqueuesTheNotificationWithTheActorSubject()
    {
        var fixture = new NotifierFixture(RealtimeNotifierTestData.UserActor);

        fixture.Notifier.AddToUser(
            RealtimeNotifierTestData.OtherUserId,
            RealtimeNotificationKind.ShiftChanged,
            new { Name = "data" });
        fixture.Notifier.Push();

        var push = fixture.Queue.Items.Should().ContainSingle().Subject.Push;
        push.Target.Should().Be(RealtimePushTarget.User);
        push.UserId.Should().Be(RealtimeNotifierTestData.OtherUserId);
        push.Notification!.ActorSubject.Should().Be(RealtimeNotifierTestData.ActorUserId);
        push.Notification.OccurredAt.Should().Be(RealtimeNotifierTestData.Now);
    }

    [Fact]
    public void AddToUser_FromAServiceActor_EnqueuesTheNotificationWithoutActorSubject()
    {
        var fixture = new NotifierFixture(RealtimeNotifierTestData.ServiceActor);

        fixture.Notifier.AddToUser(
            RealtimeNotifierTestData.ActorUserId,
            RealtimeNotificationKind.IngestFinished,
            new { Name = "data" });
        fixture.Notifier.Push();

        var push = fixture.Queue.Items.Should().ContainSingle().Subject.Push;
        push.Notification!.ActorSubject.Should().BeNull();
    }

    [Fact]
    public void AddToUser_WithGroupKind_Throws()
    {
        var fixture = new NotifierFixture(RealtimeNotifierTestData.ServiceActor);

        var act = () => fixture.Notifier.AddToUser(
            RealtimeNotifierTestData.OtherUserId,
            RealtimeNotificationKind.WorkOrderBlocked,
            new { Name = "data" });

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(expectedWildcardPattern: "*workOrderBlocked*AddToGroup*");
    }

    [Fact]
    public void AddToGroup_WithUserKind_Throws()
    {
        var fixture = new NotifierFixture(RealtimeNotifierTestData.ServiceActor);

        var act = () => fixture.Notifier.AddToGroup(
            RealtimeNotifierTestData.CompanyId,
            RealtimeNotificationKind.ShiftChanged,
            new { Name = "data" });

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(expectedWildcardPattern: "*shiftChanged*AddToUser*");
    }

    [Fact]
    public void Push_AfterPushing_EmptiesTheBuffer()
    {
        var fixture = new NotifierFixture(RealtimeNotifierTestData.ServiceActor);
        fixture.Notifier.AddSessionEnd(RealtimeNotifierTestData.OtherUserId);
        fixture.Notifier.Push();
        fixture.Queue.Clear();

        fixture.Notifier.Push();

        fixture.Queue.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clear_WithBufferedEvents_DropsThem()
    {
        var fixture = new NotifierFixture(RealtimeNotifierTestData.ServiceActor);
        fixture.Notifier.AddSessionEnd(RealtimeNotifierTestData.OtherUserId);

        fixture.Notifier.Clear();
        fixture.Notifier.Push();

        fixture.Queue.Items.Should().BeEmpty();
    }
}
