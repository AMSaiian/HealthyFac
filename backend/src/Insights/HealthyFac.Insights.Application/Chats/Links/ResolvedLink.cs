using HealthyFac.Insights.Domain.Chats.Messages;

namespace HealthyFac.Insights.Application.Chats.Links;

/// <summary>
/// A link of a new message after the send resolved it as the user: the label that the message stores, and the context
/// block that the model of this turn reads.
/// </summary>
public sealed record ResolvedLink
{
    public required ChatLinkKind Kind { get; init; }

    public required Guid EntityId { get; init; }

    public required int Position { get; init; }

    /// <summary>
    /// The name of the entity at the time of the send. It is never blank and has at most the label limit.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>The data of the entity inside an escaped data block.</summary>
    public required string ContextBlock { get; init; }
}
