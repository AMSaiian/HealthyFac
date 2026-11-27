using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Shared.UnitTests.Realtime.Data;

public static class ChatPushTestData
{
    public static readonly Guid OwnerId = Guid.Parse(input: "66666666-6666-6666-6666-666666666666");

    public static ChatTurnUpdate Update() =>
        new()
        {
            ChatId = Guid.Parse(input: "77777777-7777-7777-7777-777777777777"),
            MessageId = Guid.Parse(input: "88888888-8888-8888-8888-888888888888"),
            Sequence = 3,
            Text = "Check the filter",
            Activity = "thinking",
        };

    public static ChatTurnEnd End() =>
        new()
        {
            ChatId = Guid.Parse(input: "77777777-7777-7777-7777-777777777777"),
            MessageId = Guid.Parse(input: "88888888-8888-8888-8888-888888888888"),
            State = "completed",
        };
}
