namespace HealthyFac.Insights.Messaging.Drafts;

/// <summary>
/// One proposed machine with the definitions that belong to it. <see cref="DraftsProposedFlattening"/> turns the
/// groups into the flat lists of <see cref="DraftsProposed"/>.
/// </summary>
public sealed record ProposedMachineGroup
{
    public required Guid DraftId { get; init; }

    /// <summary>The existing machine of a machine file, or <see langword="null"/> in a company file.</summary>
    public Guid? TargetMachineId { get; init; }

    public string? Name { get; init; }

    public string? SerialNumber { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<ProposedMetric> Metrics { get; init; } = [];

    public IReadOnlyList<ProposedStatus> Statuses { get; init; } = [];

    public IReadOnlyList<ProposedPhase> Phases { get; init; } = [];
}
