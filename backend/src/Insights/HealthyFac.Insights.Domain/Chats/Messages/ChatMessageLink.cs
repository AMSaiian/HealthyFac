using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Chats.Messages;

/// <summary>
/// A link of a user message to a machine, a case, a file, an employee, or a work order. The label is the name that the
/// user saw when they picked the entity.
/// </summary>
public class ChatMessageLink : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Creates a link.
    /// </summary>
    /// <param name="kind">What the link points to.</param>
    /// <param name="entityId">The ID of the linked entity.</param>
    /// <param name="label">The name of the entity for the text of the message.</param>
    /// <param name="position">The place of the link in the text of the message.</param>
    public ChatMessageLink(ChatLinkKind kind, Guid entityId, string label, int position)
    {
        Kind = kind;
        EntityId = entityId;
        Label = label;
        Position = position;
    }

    private ChatMessageLink()
    {
        Kind = null!;
        Label = null!;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid ChatMessageId { get; private set; }

    public ChatLinkKind Kind { get; private set; }

    public Guid EntityId { get; private set; }

    public string Label { get; private set; }

    public int Position { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }
}
