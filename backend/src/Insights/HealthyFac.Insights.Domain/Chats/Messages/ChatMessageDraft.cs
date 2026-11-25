using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Chats.Messages;

/// <summary>
/// The link of an assistant message to a draft that the reply proposed. The draft lives in Core or Telemetry. The row
/// keeps its ID, its title, and the target machine, so the chat shows it without a call to the owner.
/// </summary>
public class ChatMessageDraft : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Creates a draft link.
    /// </summary>
    /// <param name="kind">The kind of the draft.</param>
    /// <param name="draftId">The ID of the draft in its service.</param>
    /// <param name="title">The title that the chat shows.</param>
    /// <param name="targetMachineId">The machine that the draft changes, or <see langword="null"/>.</param>
    public ChatMessageDraft(ChatDraftKind kind, Guid draftId, string title, Guid? targetMachineId)
    {
        Kind = kind;
        DraftId = draftId;
        Title = title;
        TargetMachineId = targetMachineId;
    }

    private ChatMessageDraft()
    {
        Kind = null!;
        Title = null!;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid ChatMessageId { get; private set; }

    public ChatDraftKind Kind { get; private set; }

    public Guid DraftId { get; private set; }

    public string Title { get; private set; }

    public Guid? TargetMachineId { get; private set; }

    /// <summary>Tells whether a user applied the draft.</summary>
    public bool IsCommitted { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Marks the draft as applied.
    /// </summary>
    public void Commit() => IsCommitted = true;
}
