using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Chats.Messages;

/// <summary>
/// Defines the kind of an assistant message.
/// The camelCase values reach the API and the database, so never rename one in place.
/// </summary>
public sealed class ChatMessageKind : Enumeration<ChatMessageKind>
{
    public static readonly ChatMessageKind Answer = new(value: "answer");
    public static readonly ChatMessageKind Refusal = new(value: "refusal");
    public static readonly ChatMessageKind Unanswerable = new(value: "unanswerable");
    public static readonly ChatMessageKind BlockedNotice = new(value: "blockedNotice");

    private ChatMessageKind(string value)
        : base(value)
    {
    }
}
