using HealthyFac.Insights.Domain.Chats.Messages;

namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// One message of the history that the chat model reads.
/// </summary>
public sealed record ChatPromptMessage
{
    public required ChatMessageRole Role { get; init; }

    public required string Text { get; init; }
}
