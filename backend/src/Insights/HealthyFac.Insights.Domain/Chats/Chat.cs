using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Chats;

/// <summary>
/// A conversation of one user with the assistant. The chat owns its messages, and a message has a sequence that is
/// unique in the chat. A <c>blocked</c> chat takes no new message. The summary replaces the old messages for the model.
/// </summary>
public class Chat : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    /// <summary>The longest title. The title is the start of the first message.</summary>
    public const int TitleMaxLength = 80;

    private readonly List<ChatMessage> _messages = [];

    /// <summary>
    /// Creates an open chat.
    /// </summary>
    /// <param name="ownerUserId">The user who owns the chat.</param>
    /// <param name="companyId">The company of the user.</param>
    /// <param name="now">The creation time (UTC). It is the first last activity time.</param>
    public Chat(Guid ownerUserId, Guid companyId, DateTime now)
    {
        LastActivityAt = now;
        OwnerUserId = ownerUserId;
        CompanyId = companyId;
        State = ChatState.Open;
        Title = string.Empty;
    }

    private Chat()
    {
        State = null!;
        Title = null!;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public uint RowVersion { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Title { get; private set; }

    public ChatState State { get; private set; }

    /// <summary>
    /// The summary of the messages up to <see cref="SummaryThroughSequence"/>, or <see langword="null"/>.
    /// </summary>
    public string? Summary { get; private set; }

    public int? SummaryThroughSequence { get; private set; }

    /// <summary>Tells that a deleted message was part of the summary, so the next turn writes it again.</summary>
    public bool IsSummaryStale { get; private set; }

    /// <summary>
    /// The time of the last turn or message delete (UTC). Unlike <c>UpdatedAt</c>, it is never <see langword="null"/>,
    /// so the chat list sorts and pages by it with an index.
    /// </summary>
    public DateTime LastActivityAt { get; private set; }

    /// <summary>The sequence that the next message gets.</summary>
    public int NextSequence { get; private set; } = 1;

    public DateTime CreatedAt { get; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    /// <summary>The messages of the chat. A query loads them with <c>Include</c>.</summary>
    public IReadOnlyCollection<ChatMessage> Messages => _messages;

    /// <summary>
    /// Adds a turn: the message of the user and the reply that starts to stream. The first turn names the chat.
    /// </summary>
    /// <param name="text">The text of the user.</param>
    /// <param name="links">The links of the message.</param>
    /// <param name="now">The time of the turn (UTC).</param>
    /// <returns>The message of the user and the message of the assistant.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="ChatInvariantErrors"/> when the chat is blocked.
    /// </exception>
    public (ChatMessage User, ChatMessage Assistant) AddTurn(
        string text,
        IEnumerable<ChatMessageLink> links,
        DateTime now
    )
    {
        if (State == ChatState.Blocked)
        {
            throw ChatInvariantErrors.ChatBlocked(Id);
        }

        if (Title.Length == 0)
        {
            Title = text.Length <= TitleMaxLength ? text : CutTitle(text);
        }

        LastActivityAt = now;

        var user = new ChatMessage(
            NextSequence,
            ChatMessageRole.User,
            text,
            ChatMessageState.Completed,
            now,
            links);

        var assistant = new ChatMessage(
            NextSequence + 1,
            ChatMessageRole.Assistant,
            string.Empty,
            ChatMessageState.Streaming,
            now,
            []);

        _messages.Add(user);
        _messages.Add(assistant);
        NextSequence += 2;

        return (user, assistant);
    }

    /// <summary>
    /// Blocks the chat: it takes no new message.
    /// </summary>
    public void Block() => State = ChatState.Blocked;

    /// <summary>
    /// Stores the summary of the old messages.
    /// </summary>
    /// <param name="text">The summary.</param>
    /// <param name="throughSequence">The last sequence that the summary covers.</param>
    public void SetSummary(string text, int throughSequence)
    {
        Summary = text;
        SummaryThroughSequence = throughSequence;
        IsSummaryStale = false;
    }

    /// <summary>
    /// Finds the user message and its reply, and marks the summary as stale when it covers the message. The caller
    /// removes the returned messages, and the soft delete interceptor turns the removal into an update.
    /// </summary>
    /// <param name="messageId">The ID of the user message.</param>
    /// <param name="now">
    /// The time of the delete (UTC). The change of the chat row makes the save check its row version.
    /// </param>
    /// <returns>The user message and its reply.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="ChatInvariantErrors"/> when the message is missing, is not a user message, or its reply
    /// still streams.
    /// </exception>
    public IReadOnlyList<ChatMessage> DeleteUserMessage(Guid messageId, DateTime now)
    {
        var message = _messages.SingleOrDefault(candidate => candidate.Id == messageId && candidate.DeletedAt is null)
            ?? throw ChatInvariantErrors.MessageNotFound(Id, messageId);

        if (message.Role != ChatMessageRole.User)
        {
            throw ChatInvariantErrors.NotAUserMessage(Id, messageId);
        }

        var reply = _messages.SingleOrDefault(candidate =>
            candidate.Sequence == message.Sequence + 1 && candidate.DeletedAt is null);

        if (reply is { IsStreaming: true })
        {
            throw ChatInvariantErrors.TurnInProgress(Id, messageId);
        }

        LastActivityAt = now;

        if (SummaryThroughSequence is { } through && message.Sequence <= through)
        {
            IsSummaryStale = true;
        }

        return reply is null ? [message] : [message, reply];
    }

    private static string CutTitle(string text) =>
        char.IsHighSurrogate(text[TitleMaxLength - 1]) ? text[..(TitleMaxLength - 1)] : text[..TitleMaxLength];
}
