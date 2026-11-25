namespace HealthyFac.Insights.Application.Chats.Drafts;

/// <summary>
/// A work order that the model proposed in one chat turn. The final save of a completed turn sends it as a
/// <c>WorkOrderDraftProposed</c> message of the source <c>chat</c>.
/// </summary>
public sealed record ChatWorkOrderProposal
{
    public required Guid DraftId { get; init; }

    /// <summary>The existing machine of the work order, or <see langword="null"/>.</summary>
    public Guid? MachineId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required string Priority { get; init; }

    public required string Category { get; init; }

    public required int EstimateMinutes { get; init; }
}
