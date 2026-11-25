using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Seeder.Ingest;

/// <summary>
/// The body of <see cref="DraftsProposed"/> in the nested form of the seed file. The job fills the attempt, the file,
/// the company, and the queue time, and flattens the definitions with <see cref="ToMessage"/>.
/// </summary>
public sealed class IngestDraftsSeed
{
    public required bool IsMachineFile { get; init; }

    public required IReadOnlyList<IngestMachineSeed> Machines { get; init; }

    /// <summary>
    /// Builds the message of the source <c>ingest</c> with <see cref="DraftsProposedFlattening"/>, the rule that the
    /// extraction buffer uses too.
    /// </summary>
    /// <param name="attemptId">The ingest attempt.</param>
    /// <param name="fileId">The file.</param>
    /// <param name="companyId">The company.</param>
    /// <param name="queuedAt">The time when the ingest was queued (UTC).</param>
    /// <returns>The message.</returns>
    public DraftsProposed ToMessage(Guid attemptId, Guid fileId, Guid companyId, DateTime queuedAt)
    {
        IReadOnlyList<ProposedMachineGroup> groups = [.. Machines.Select(machine => machine.ToGroup())];

        return new DraftsProposed
        {
            Source = ProposalSource.Ingest,
            AttemptId = attemptId,
            FileId = fileId,
            CompanyId = companyId,
            IsMachineFile = IsMachineFile,
            QueuedAt = queuedAt,
            Machines = DraftsProposedFlattening.Machines(groups, IsMachineFile),
            Metrics = DraftsProposedFlattening.Metrics(groups, IsMachineFile),
            Statuses = DraftsProposedFlattening.Statuses(groups, IsMachineFile),
            Phases = DraftsProposedFlattening.Phases(groups, IsMachineFile),
        };
    }
}
