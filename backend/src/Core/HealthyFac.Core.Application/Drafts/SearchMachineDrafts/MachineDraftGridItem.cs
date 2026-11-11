using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Application.Drafts.SearchMachineDrafts;

/// <summary>
/// A row of the draft grid of a machine: one pending draft of any kind. The row implements <see cref="IEntity{TId}"/>
/// so the shared grid helpers (filter, sort, page) work on the union of the three draft tables.
/// </summary>
public record MachineDraftGridItem : IEntity<Guid>
{
    public required Guid Id { get; init; }

    /// <summary>One of <see cref="DraftKinds.All"/>.</summary>
    public required string Kind { get; init; }

    public required Guid AttemptId { get; init; }

    public required Guid FileId { get; init; }

    public required DateTime ProposedAt { get; init; }

    public string? SourceLocation { get; init; }

    /// <summary>A short label: the name or serial number, the metric name or key, or the status code.</summary>
    public string? Label { get; init; }

    /// <summary>The row version of the draft.</summary>
    public required uint RowVersion { get; init; }

    /// <summary>
    /// Builds the union query of the pending drafts of one machine. Each branch projects into the row type inside the
    /// query, so the database reads only these columns.
    /// </summary>
    /// <param name="machineDrafts">The machine drafts.</param>
    /// <param name="metricDrafts">The metric drafts.</param>
    /// <param name="statusDrafts">The status drafts.</param>
    /// <param name="companyId">The company of the actor.</param>
    /// <param name="machineId">The target machine.</param>
    /// <returns>The query over the three tables.</returns>
    public static IQueryable<MachineDraftGridItem> Union(
        IQueryable<MachineDraft> machineDrafts,
        IQueryable<MetricDefinitionDraft> metricDrafts,
        IQueryable<StatusDefinitionDraft> statusDrafts,
        Guid companyId,
        Guid machineId
    )
    {
        var machines = machineDrafts
            .Where(draft => draft.CompanyId == companyId
                && draft.TargetMachineId == machineId
                && draft.State == DraftState.Pending)
            .Select(draft => new MachineDraftGridItem
            {
                Id = draft.Id,
                Kind = DraftKinds.Machine,
                AttemptId = draft.AttemptId,
                FileId = draft.FileId,
                ProposedAt = draft.CreatedAt,
                SourceLocation = null,
                Label = draft.Name ?? draft.SerialNumber,
                RowVersion = draft.RowVersion,
            });
        var metrics = metricDrafts
            .Where(draft => draft.CompanyId == companyId
                && draft.TargetMachineId == machineId
                && draft.State == DraftState.Pending)
            .Select(draft => new MachineDraftGridItem
            {
                Id = draft.Id,
                Kind = DraftKinds.Metric,
                AttemptId = draft.AttemptId,
                FileId = draft.FileId,
                ProposedAt = draft.CreatedAt,
                SourceLocation = draft.SourceLocation,
                Label = draft.Name ?? draft.Key,
                RowVersion = draft.RowVersion,
            });
        var statuses = statusDrafts
            .Where(draft => draft.CompanyId == companyId
                && draft.TargetMachineId == machineId
                && draft.State == DraftState.Pending)
            .Select(draft => new MachineDraftGridItem
            {
                Id = draft.Id,
                Kind = DraftKinds.Status,
                AttemptId = draft.AttemptId,
                FileId = draft.FileId,
                ProposedAt = draft.CreatedAt,
                SourceLocation = draft.SourceLocation,
                Label = draft.Code,
                RowVersion = draft.RowVersion,
            });

        return machines.Concat(metrics).Concat(statuses);
    }
}
