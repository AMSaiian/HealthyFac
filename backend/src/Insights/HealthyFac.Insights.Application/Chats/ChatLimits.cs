using HealthyFac.Insights.Domain.Chats;

namespace HealthyFac.Insights.Application.Chats;

/// <summary>
/// Defines the limits of a chat. Each value is defined once, here or in the Domain layer.
/// </summary>
public static class ChatLimits
{
    /// <summary>The most characters of a message of the user.</summary>
    public const int TextMaxLength = 2000;

    /// <summary>The most links of one message.</summary>
    public const int LinksMaxCount = 10;

    /// <summary>The number of messages after the summary that starts the compaction.</summary>
    public const int CompactionThreshold = 30;

    /// <summary>The number of last messages that stay word for word when the chat is compacted.</summary>
    public const int KeptMessageCount = 10;

    /// <summary>The most characters of the title of a chat.</summary>
    public const int TitleMaxLength = Chat.TitleMaxLength;
}
