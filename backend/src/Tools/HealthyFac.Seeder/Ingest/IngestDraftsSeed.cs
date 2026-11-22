using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Seeder.Ingest;

/// <summary>
/// The body of <see cref="DraftsProposed"/>. The job fills the attempt, the file, the company, and the queue time.
/// </summary>
public sealed class IngestDraftsSeed
{
    public required bool IsMachineFile { get; init; }

    public required IReadOnlyList<ProposedMachine> Machines { get; init; }
}
