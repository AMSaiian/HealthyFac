namespace HealthyFac.Seeder.Cases;

/// <summary>The body of <c>WorkOrderDraftProposed</c>. The job fills the company, the machine, and the case.</summary>
public sealed class WorkOrderDraftSeed
{
    public required Guid DraftId { get; init; }

    public required Guid RunId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required string Priority { get; init; }

    public required string Category { get; init; }

    public required int EstimateMinutes { get; init; }
}
