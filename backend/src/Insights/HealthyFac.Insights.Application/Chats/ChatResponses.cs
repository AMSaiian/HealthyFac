using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Application.Chats;

/// <summary>
/// A row of the chat list. The row implements <see cref="IEntity{TId}"/>, so the shared keyset helpers work on the
/// projected row.
/// </summary>
public record ChatListItem : IEntity<Guid>, IKeysetGridItem<ChatListItem>
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required ChatState State { get; init; }

    /// <summary>The time of the last turn or message delete of the chat (UTC).</summary>
    public required DateTime UpdatedAt { get; init; }

    public KeysetPosition? Position { get; init; }

    public ChatListItem WithPosition(KeysetPosition position) => this with { Position = position };
}

/// <summary>
/// The result of a send: the chat with its new state, the message of the user, and the final reply.
/// </summary>
public record ChatTurnResponse
{
    public required ChatResponse Chat { get; init; }

    public required ChatMessageResponse User { get; init; }

    public required ChatMessageResponse Assistant { get; init; }
}

public record ChatResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required ChatState State { get; init; }

    public required int MessageCount { get; init; }
}

/// <summary>
/// One page of the messages of a chat, the newest first. <see cref="NextBefore"/> is the sequence to send as
/// <c>before</c> for the next page, or <see langword="null"/> on the last page.
/// </summary>
public record ChatMessagesResponse
{
    public required IReadOnlyList<ChatMessageResponse> Items { get; init; }

    public int? NextBefore { get; init; }
}

public record ChatMessageResponse
{
    public required Guid Id { get; init; }

    public required int Sequence { get; init; }

    public required ChatMessageRole Role { get; init; }

    public ChatMessageKind? Kind { get; init; }

    public required ChatMessageState State { get; init; }

    public required string Text { get; init; }

    public AgentRunErrorCode? ErrorCode { get; init; }

    public required IReadOnlyList<ChatLinkResponse> Links { get; init; }

    public required IReadOnlyList<ChatDraftResponse> Drafts { get; init; }
}

public record ChatLinkResponse
{
    public required ChatLinkKind Kind { get; init; }

    public required Guid EntityId { get; init; }

    public required string Label { get; init; }

    public required int Position { get; init; }
}

public record ChatDraftResponse
{
    public required ChatDraftKind Kind { get; init; }

    public required Guid DraftId { get; init; }

    public required string Title { get; init; }

    public Guid? TargetMachineId { get; init; }

    public required bool IsCommitted { get; init; }
}
