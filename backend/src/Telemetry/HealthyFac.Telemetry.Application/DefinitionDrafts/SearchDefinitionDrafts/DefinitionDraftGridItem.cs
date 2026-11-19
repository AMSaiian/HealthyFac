using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;

/// <summary>
/// A row of the definition draft grid: one metric, status, or phase draft in any state. The row implements
/// <see cref="IEntity{TId}"/> so the shared grid helpers (filter, sort, page) work on the union of the three tables.
/// </summary>
public record DefinitionDraftGridItem : IEntity<Guid>, IKeysetGridItem<DefinitionDraftGridItem>
{
    public required Guid Id { get; init; }

    /// <summary>One of <see cref="DefinitionDraftKinds.All"/>.</summary>
    public required string Kind { get; init; }

    public required DefinitionDraftState State { get; init; }

    public required Guid AttemptId { get; init; }

    public required Guid FileId { get; init; }

    public required DateTime ProposedAt { get; init; }

    public string? SourceLocation { get; init; }

    /// <summary>A short label: the metric name or key, or the status code.</summary>
    public string? Label { get; init; }

    /// <summary>The row version of the draft.</summary>
    public required uint RowVersion { get; init; }

    /// <summary>The machine that the draft targets, or <see langword="null"/> while it has no machine yet.</summary>
    public Guid? MachineId { get; init; }

    /// <summary>The pending create machine draft that the draft waits on, or <see langword="null"/>.</summary>
    public Guid? TargetMachineDraftId { get; init; }

    /// <summary>The keyset position of the row under the sort of the request.</summary>
    public KeysetPosition? Position { get; init; }

    public DefinitionDraftGridItem WithPosition(KeysetPosition position) => this with { Position = position };

    /// <summary>
    /// Builds the union query of the definition drafts in one scope: the drafts that target one machine, or the
    /// drafts of one ingest attempt. Each branch projects into the row type inside the query, so the database reads
    /// only these columns.
    /// </summary>
    /// <param name="metricDrafts">The metric drafts.</param>
    /// <param name="statusDrafts">The status drafts.</param>
    /// <param name="phaseDrafts">The phase drafts.</param>
    /// <param name="companyId">The company of the actor.</param>
    /// <param name="machineId">The target machine, or <see langword="null"/> to scope by the attempt.</param>
    /// <param name="attemptId">The ingest attempt, or <see langword="null"/> to scope by the machine.</param>
    /// <returns>The query over the three tables.</returns>
    public static IQueryable<DefinitionDraftGridItem> Union(
        IQueryable<MetricDefinitionDraft> metricDrafts,
        IQueryable<StatusDefinitionDraft> statusDrafts,
        IQueryable<OperatingPhaseDraft> phaseDrafts,
        Guid companyId,
        Guid? machineId,
        Guid? attemptId
    )
    {
        var metricRows = metricDrafts
            .Where(draft => draft.CompanyId == companyId
                && (machineId == null || draft.TargetMachineId == machineId)
                && (attemptId == null || draft.AttemptId == attemptId))
            .Select(draft => new DefinitionDraftGridItem
            {
                Id = draft.Id,
                Kind = DefinitionDraftKinds.Metric,
                State = draft.State,
                AttemptId = draft.AttemptId,
                FileId = draft.FileId,
                ProposedAt = draft.CreatedAt,
                SourceLocation = draft.SourceLocation,
                Label = draft.Name ?? draft.Key,
                RowVersion = draft.RowVersion,
                MachineId = draft.TargetMachineId,
                TargetMachineDraftId = draft.TargetMachineDraftId,
            });
        var statusRows = statusDrafts
            .Where(draft => draft.CompanyId == companyId
                && (machineId == null || draft.TargetMachineId == machineId)
                && (attemptId == null || draft.AttemptId == attemptId))
            .Select(draft => new DefinitionDraftGridItem
            {
                Id = draft.Id,
                Kind = DefinitionDraftKinds.Status,
                State = draft.State,
                AttemptId = draft.AttemptId,
                FileId = draft.FileId,
                ProposedAt = draft.CreatedAt,
                SourceLocation = draft.SourceLocation,
                Label = draft.Code,
                RowVersion = draft.RowVersion,
                MachineId = draft.TargetMachineId,
                TargetMachineDraftId = draft.TargetMachineDraftId,
            });

        var phaseRows = phaseDrafts
            .Where(draft => draft.CompanyId == companyId
                && (machineId == null || draft.TargetMachineId == machineId)
                && (attemptId == null || draft.AttemptId == attemptId))
            .Select(draft => new DefinitionDraftGridItem
            {
                Id = draft.Id,
                Kind = DefinitionDraftKinds.Phase,
                State = draft.State,
                AttemptId = draft.AttemptId,
                FileId = draft.FileId,
                ProposedAt = draft.CreatedAt,
                SourceLocation = draft.SourceLocation,
                Label = draft.Name ?? draft.Code,
                RowVersion = draft.RowVersion,
                MachineId = draft.TargetMachineId,
                TargetMachineDraftId = draft.TargetMachineDraftId,
            });

        return metricRows.Concat(statusRows).Concat(phaseRows);
    }
}
