using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Application.Drafts.SearchMachineDrafts;

/// <summary>
/// A row of the draft grid: one draft of any kind and any state. The row implements <see cref="IEntity{TId}"/> so the
/// shared grid helpers (filter, sort, page) work on the union of the three draft tables.
/// </summary>
public record MachineDraftGridItem : IEntity<Guid>
{
    public required Guid Id { get; init; }

    /// <summary>One of <see cref="DraftKinds.All"/>.</summary>
    public required string Kind { get; init; }

    public required DraftState State { get; init; }

    public required Guid AttemptId { get; init; }

    public required Guid FileId { get; init; }

    public required DateTime ProposedAt { get; init; }

    public string? SourceLocation { get; init; }

    /// <summary>A short label: the name or serial number, the metric name or key, or the status code.</summary>
    public string? Label { get; init; }

    /// <summary>The row version of the draft.</summary>
    public required uint RowVersion { get; init; }

    /// <summary>Tells whether the row is a machine draft that proposes a new machine.</summary>
    public required bool IsCreate { get; init; }

    /// <summary>
    /// The machine that the draft targets, or <see langword="null"/> while a create draft is pending. A child draft of
    /// a pending create draft has no machine yet, and its review waits for the apply of the create draft.
    /// </summary>
    public Guid? MachineId { get; init; }

    /// <summary>The name of the machine, or the proposed name of the pending create draft.</summary>
    public string? MachineName { get; init; }

    /// <summary>
    /// Builds the union query of the drafts in one scope: the drafts that target one machine, or the drafts of one
    /// ingest attempt. Each branch projects into the row type inside the query, so the database reads only these
    /// columns.
    /// </summary>
    /// <param name="machineDrafts">The machine drafts.</param>
    /// <param name="metricDrafts">The metric drafts.</param>
    /// <param name="statusDrafts">The status drafts.</param>
    /// <param name="machines">The machines, to read the machine name.</param>
    /// <param name="companyId">The company of the actor.</param>
    /// <param name="machineId">The target machine, or <see langword="null"/> to scope by the attempt.</param>
    /// <param name="attemptId">The ingest attempt, or <see langword="null"/> to scope by the machine.</param>
    /// <returns>The query over the three tables.</returns>
    public static IQueryable<MachineDraftGridItem> Union(
        IQueryable<MachineDraft> machineDrafts,
        IQueryable<MetricDefinitionDraft> metricDrafts,
        IQueryable<StatusDefinitionDraft> statusDrafts,
        IQueryable<Machine> machines,
        Guid companyId,
        Guid? machineId,
        Guid? attemptId
    )
    {
        var companyMachineDrafts = machineDrafts.Where(draft => draft.CompanyId == companyId);
        var companyMachines = machines.Where(machine => machine.CompanyId == companyId);

        var machineRows = companyMachineDrafts
            .Where(draft => (machineId == null || draft.TargetMachineId == machineId)
                && (attemptId == null || draft.AttemptId == attemptId))
            .Select(draft => new MachineDraftGridItem
            {
                Id = draft.Id,
                Kind = DraftKinds.Machine,
                State = draft.State,
                AttemptId = draft.AttemptId,
                FileId = draft.FileId,
                ProposedAt = draft.CreatedAt,
                SourceLocation = null,
                Label = draft.Name ?? draft.SerialNumber,
                RowVersion = draft.RowVersion,
                IsCreate = draft.TargetMachineId == null,
                MachineId = draft.TargetMachineId ?? draft.AppliedMachineId,
                MachineName = companyMachines
                    .Where(machine => machine.Id == (draft.TargetMachineId ?? draft.AppliedMachineId))
                    .Select(machine => machine.Name)
                    .FirstOrDefault() ?? draft.Name,
            });
        var metricRows = metricDrafts
            .Where(draft => draft.CompanyId == companyId
                && (machineId == null || draft.TargetMachineId == machineId)
                && (attemptId == null || draft.AttemptId == attemptId))
            .Select(draft => new MachineDraftGridItem
            {
                Id = draft.Id,
                Kind = DraftKinds.Metric,
                State = draft.State,
                AttemptId = draft.AttemptId,
                FileId = draft.FileId,
                ProposedAt = draft.CreatedAt,
                SourceLocation = draft.SourceLocation,
                Label = draft.Name ?? draft.Key,
                RowVersion = draft.RowVersion,
                IsCreate = false,
                MachineId = draft.TargetMachineId,
                MachineName = companyMachines
                    .Where(machine => machine.Id == draft.TargetMachineId)
                    .Select(machine => machine.Name)
                    .FirstOrDefault()
                    ?? companyMachineDrafts
                        .Where(machineDraft => machineDraft.Id == draft.TargetMachineDraftId)
                        .Select(machineDraft => machineDraft.Name)
                        .FirstOrDefault(),
            });
        var statusRows = statusDrafts
            .Where(draft => draft.CompanyId == companyId
                && (machineId == null || draft.TargetMachineId == machineId)
                && (attemptId == null || draft.AttemptId == attemptId))
            .Select(draft => new MachineDraftGridItem
            {
                Id = draft.Id,
                Kind = DraftKinds.Status,
                State = draft.State,
                AttemptId = draft.AttemptId,
                FileId = draft.FileId,
                ProposedAt = draft.CreatedAt,
                SourceLocation = draft.SourceLocation,
                Label = draft.Code,
                RowVersion = draft.RowVersion,
                IsCreate = false,
                MachineId = draft.TargetMachineId,
                MachineName = companyMachines
                    .Where(machine => machine.Id == draft.TargetMachineId)
                    .Select(machine => machine.Name)
                    .FirstOrDefault()
                    ?? companyMachineDrafts
                        .Where(machineDraft => machineDraft.Id == draft.TargetMachineDraftId)
                        .Select(machineDraft => machineDraft.Name)
                        .FirstOrDefault(),
            });

        return machineRows.Concat(metricRows).Concat(statusRows);
    }
}
