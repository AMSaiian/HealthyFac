using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Chats.Messages;

/// <summary>
/// One message of a chat. A user message is <c>completed</c> from the start. An assistant message starts
/// <c>streaming</c> and ends once, in one of the final states.
/// </summary>
public class ChatMessage : IEntity, IAuditable, ISoftDeletable
{
    private readonly List<ChatMessageLink> _links = [];
    private readonly List<ChatMessageDraft> _drafts = [];

    /// <summary>
    /// Creates a message. The <see cref="Chat"/> creates its messages.
    /// </summary>
    /// <param name="sequence">The place of the message in the chat.</param>
    /// <param name="role">Who wrote the message.</param>
    /// <param name="text">The text, empty for an assistant message that starts.</param>
    /// <param name="state">The first state.</param>
    /// <param name="startedAt">The time of the start (UTC).</param>
    /// <param name="links">The links of a user message.</param>
    internal ChatMessage(
        int sequence,
        ChatMessageRole role,
        string text,
        ChatMessageState state,
        DateTime startedAt,
        IEnumerable<ChatMessageLink> links
    )
    {
        Sequence = sequence;
        Role = role;
        Text = text;
        State = state;
        StartedAt = startedAt;
        _links.AddRange(links);
    }

    private ChatMessage()
    {
        Role = null!;
        State = null!;
        Text = null!;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid ChatId { get; private set; }

    public int Sequence { get; private set; }

    public ChatMessageRole Role { get; private set; }

    /// <summary>The kind of a completed assistant message, or <see langword="null"/>.</summary>
    public ChatMessageKind? Kind { get; private set; }

    public ChatMessageState State { get; private set; }

    public string Text { get; private set; }

    public Guid? AgentRunId { get; private set; }

    public AgentRunErrorCode? ErrorCode { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public IReadOnlyCollection<ChatMessageLink> Links => _links;

    public IReadOnlyCollection<ChatMessageDraft> Drafts => _drafts;

    /// <summary>Tells whether the message is an assistant message that still streams.</summary>
    public bool IsStreaming => State == ChatMessageState.Streaming;

    /// <summary>
    /// Sets the text of the reply that streams.
    /// </summary>
    /// <param name="text">The text so far.</param>
    /// <exception cref="InvalidOperationException">Thrown when the message does not stream.</exception>
    public void Update(string text)
    {
        EnsureStreaming();
        Text = text;
    }

    /// <summary>
    /// Ends the reply with its answer.
    /// </summary>
    /// <param name="kind">The kind of the answer.</param>
    /// <param name="text">The final text.</param>
    /// <exception cref="InvalidOperationException">Thrown when the message does not stream.</exception>
    public void Complete(ChatMessageKind kind, string text)
    {
        EnsureStreaming();
        Kind = kind;
        Text = text;
        State = ChatMessageState.Completed;
    }

    /// <summary>
    /// Ends the reply without an answer.
    /// </summary>
    /// <param name="state">The final state: <c>cancelled</c>, <c>failed</c>, or <c>interrupted</c>.</param>
    /// <param name="errorCode">The reason of a failure, or <see langword="null"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the message does not stream, or when <paramref name="state"/> does not end a reply without an
    /// answer.
    /// </exception>
    public void End(ChatMessageState state, AgentRunErrorCode? errorCode)
    {
        EnsureStreaming();

        if (state == ChatMessageState.Streaming || state == ChatMessageState.Completed)
        {
            throw ChatInvariantErrors.IllegalTransition(Id, State);
        }

        State = state;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Adds the link to a draft that the reply proposed.
    /// </summary>
    /// <param name="draft">The draft link.</param>
    public void AddDraft(ChatMessageDraft draft) => _drafts.Add(draft);

    /// <summary>
    /// Links the reply to the run of the agent.
    /// </summary>
    /// <param name="agentRunId">The ID of the run.</param>
    public void AttachRun(Guid agentRunId) => AgentRunId = agentRunId;

    /// <summary>
    /// Tells whether the reply streams for longer than the turn limit, so no process works on it any more.
    /// </summary>
    /// <param name="now">The current time (UTC).</param>
    /// <param name="turnLimit">The longest time of a turn.</param>
    /// <returns><see langword="true"/> when the reply is dead.</returns>
    public bool IsDead(DateTime now, TimeSpan turnLimit) => IsStreaming && now - StartedAt > turnLimit;

    private void EnsureStreaming()
    {
        if (!IsStreaming)
        {
            throw ChatInvariantErrors.IllegalTransition(Id, State, action: "change a reply that does not stream");
        }
    }
}
