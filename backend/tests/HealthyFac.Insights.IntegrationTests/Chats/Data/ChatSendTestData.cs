using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;

namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

public static class ChatSendTestData
{
    public const int TooManyLinks = 11;

    public static TheoryData<string, string, IReadOnlyList<ChatLinkInput>, string> InvalidInputs => new()
    {
        { "Empty", string.Empty, [], "text" },
        { "TooLong", new string(c: 'a', ChatLimits.TextMaxLength + 1), [], "text" },
        { "NullCharacter", "a\0b", [], "text" },
        {
            "ElevenLinks",
            ChatTestData.Question,
            [.. Enumerable.Range(start: 0, TooManyLinks).Select(_ => Link(Guid.CreateVersion7()))],
            "links"
        },
        {
            "DuplicateLink",
            ChatTestData.Question,
            [Link(ChatTestData.MachineId), Link(ChatTestData.MachineId)],
            "links"
        },
    };

    public static ChatLinkInput Link(Guid entityId) =>
        new() { Kind = ChatLinkKind.Machine, EntityId = entityId, Position = 0 };
}
