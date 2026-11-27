using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Seeder.Ingest;

/// <summary>
/// One proposed machine of the seed file with the definitions that the extraction proposed for it. The seed file keeps
/// the definitions under their machine. The job flattens them into the <see cref="DraftsProposed"/> message.
/// </summary>
public sealed class IngestMachineSeed
{
    public required Guid DraftId { get; init; }

    public Guid? TargetMachineId { get; init; }

    public string? Name { get; init; }

    public string? SerialNumber { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<ProposedMetric> Metrics { get; init; } = [];

    public IReadOnlyList<ProposedStatus> Statuses { get; init; } = [];

    public IReadOnlyList<ProposedPhase> Phases { get; init; } = [];

    /// <summary>
    /// Builds the group that <see cref="DraftsProposedFlattening"/> turns into the lists of the message.
    /// </summary>
    /// <returns>The group.</returns>
    public ProposedMachineGroup ToGroup() =>
        new()
        {
            DraftId = DraftId,
            TargetMachineId = TargetMachineId,
            Name = Name,
            SerialNumber = SerialNumber,
            Description = Description,
            Metrics = Metrics,
            Statuses = Statuses,
            Phases = Phases,
        };
}
