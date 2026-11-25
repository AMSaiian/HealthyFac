using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Chats.Messages;

/// <summary>
/// Defines the states of a message. A user message is always <c>completed</c>.
/// An assistant message starts <c>streaming</c> and moves only forward.
/// The camelCase values reach the API and the database, so never rename one in place.
/// </summary>
public sealed class ChatMessageState : Enumeration<ChatMessageState>
{
    public static readonly ChatMessageState Streaming = new(value: "streaming");
    public static readonly ChatMessageState Completed = new(value: "completed");
    public static readonly ChatMessageState Cancelled = new(value: "cancelled");
    public static readonly ChatMessageState Failed = new(value: "failed");
    public static readonly ChatMessageState Interrupted = new(value: "interrupted");

    private ChatMessageState(string value)
        : base(value)
    {
    }
}
