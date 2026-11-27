using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

/// <summary>
/// A create machine draft with the definitions that name it. The builder flattens it into the proposal message.
/// </summary>
public sealed record ProposedEntry
{
    public Guid DraftId { get; init; } = Guid.CreateVersion7();

    public IReadOnlyList<ProposedMetric> Metrics { get; init; } = [];

    public IReadOnlyList<ProposedStatus> Statuses { get; init; } = [];

    public IReadOnlyList<ProposedPhase> Phases { get; init; } = [];
}
