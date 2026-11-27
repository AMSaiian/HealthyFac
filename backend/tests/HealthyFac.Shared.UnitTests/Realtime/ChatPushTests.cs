using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.UnitTests.Realtime.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.Realtime;

[Feature(TestFeatures.Chat)]
[Feature(TestFeatures.Realtime)]
public sealed class ChatPushTests
{
    [Fact]
    public void AddChatTurnUpdate_ForTheActorUser_StillEnqueuesTheUpdateForThatUser()
    {
        var fixture = new NotifierFixture(RealtimeNotifierTestData.UserActor);

        fixture.Notifier.AddChatTurnUpdate(RealtimeNotifierTestData.ActorUserId, ChatPushTestData.Update());
        fixture.Notifier.Push();

        var push = fixture.Queue.Items.Should().ContainSingle().Subject.Push;
        push.Target.Should().Be(RealtimePushTarget.User);
        push.UserId.Should().Be(RealtimeNotifierTestData.ActorUserId);
        push.ChatTurnUpdate.Should().Be(ChatPushTestData.Update());
        push.MetricTarget.Should().Be(expected: "user");
    }

    [Fact]
    public void AddChatTurnEnd_ForAnotherUser_EnqueuesTheEndOnlyForThatUser()
    {
        var fixture = new NotifierFixture(RealtimeNotifierTestData.UserActor);

        fixture.Notifier.AddChatTurnEnd(ChatPushTestData.OwnerId, ChatPushTestData.End());
        fixture.Notifier.Push();

        var push = fixture.Queue.Items.Should().ContainSingle().Subject.Push;
        push.UserId.Should().Be(ChatPushTestData.OwnerId);
        push.ChatTurnEnd.Should().Be(ChatPushTestData.End());
        push.Group.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ForAChatTurnUpdate_CallsChatTurnUpdatedOnTheUserOnly()
    {
        using var fixture = new PushHandlerFixture();

        var push = new RealtimePush
        {
            Target = RealtimePushTarget.User,
            UserId = ChatPushTestData.OwnerId,
            ChatTurnUpdate = ChatPushTestData.Update(),
            MetricTarget = "user",
        };

        await fixture.HandleAsync(push);

        fixture.Clients.UserCalls.Should().Equal(ChatPushTestData.OwnerId.ToString());
        fixture.Client.Targets.Should().Equal(nameof(Web.Realtime.IRealtimeClient.ChatTurnUpdated));
        fixture.Client.Updates.Should().Equal(ChatPushTestData.Update());
    }

    [Fact]
    public async Task HandleAsync_ForAChatTurnEnd_CallsChatTurnEndedOnTheUserOnly()
    {
        using var fixture = new PushHandlerFixture();

        var push = new RealtimePush
        {
            Target = RealtimePushTarget.User,
            UserId = ChatPushTestData.OwnerId,
            ChatTurnEnd = ChatPushTestData.End(),
            MetricTarget = "user",
        };

        await fixture.HandleAsync(push);

        fixture.Clients.UserCalls.Should().Equal(ChatPushTestData.OwnerId.ToString());
        fixture.Client.Targets.Should().Equal(nameof(Web.Realtime.IRealtimeClient.ChatTurnEnded));
        fixture.Client.Ends.Should().Equal(ChatPushTestData.End());
    }
}
