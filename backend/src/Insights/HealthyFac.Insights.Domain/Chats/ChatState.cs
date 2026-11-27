using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Chats;

/// <summary>
/// Defines the states of a chat.
/// A <c>blocked</c> chat takes no new message.
/// The camelCase values reach the API and the database, so never rename one in place.
/// </summary>
public sealed class ChatState : Enumeration<ChatState>
{
    public static readonly ChatState Open = new(value: "open");
    public static readonly ChatState Blocked = new(value: "blocked");

    private ChatState(string value)
        : base(value)
    {
    }
}
