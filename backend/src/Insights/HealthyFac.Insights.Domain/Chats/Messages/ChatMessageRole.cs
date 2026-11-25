using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Chats.Messages;

/// <summary>
/// Defines who wrote a message of a chat.
/// The camelCase values reach the API and the database, so never rename one in place.
/// </summary>
public sealed class ChatMessageRole : Enumeration<ChatMessageRole>
{
    public static readonly ChatMessageRole User = new(value: "user");
    public static readonly ChatMessageRole Assistant = new(value: "assistant");

    private ChatMessageRole(string value)
        : base(value)
    {
    }
}
