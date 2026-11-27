using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Application.Drafts.SearchMachineDrafts;

/// <summary>
/// A row of the draft grid: one machine draft in any state. The row implements <see cref="IEntity{TId}"/> so the
/// shared grid helpers (filter, sort, page) work on the projected row.
/// </summary>
public record MachineDraftGridItem : IEntity<Guid>, IKeysetGridItem<MachineDraftGridItem>
{
    public required Guid Id { get; init; }

    /// <summary>One of <see cref="DraftKinds.All"/>.</summary>
    public required string Kind { get; init; }

    public required DraftState State { get; init; }

    public required DraftProposalSummary Proposal { get; init; }

    public required DateTime ProposedAt { get; init; }

    public string? SourceLocation { get; init; }

    /// <summary>A short label: the name or serial number.</summary>
    public string? Label { get; init; }

    /// <summary>The row version of the draft.</summary>
    public required uint RowVersion { get; init; }

    /// <summary>Tells whether the row is a machine draft that proposes a new machine.</summary>
    public required bool IsCreate { get; init; }

    /// <summary>
    /// The machine that the draft targets, or <see langword="null"/> while a create draft is pending.
    /// </summary>
    public Guid? MachineId { get; init; }

    /// <summary>The name of the machine, or the proposed name of the pending create draft.</summary>
    public string? MachineName { get; init; }

    /// <summary>The keyset position of the row under the sort of the request.</summary>
    public KeysetPosition? Position { get; init; }

    public MachineDraftGridItem WithPosition(KeysetPosition position) => this with { Position = position };

    /// <summary>
    /// Builds the query of the machine drafts in one scope: the drafts that target one machine, the drafts of one
    /// ingest attempt, or the drafts of one chat. The query projects into the row type, so the database reads only
    /// these columns.
    /// </summary>
    /// <param name="machineDrafts">The machine drafts.</param>
    /// <param name="machines">The machines, to read the machine name.</param>
    /// <param name="companyId">The company of the actor.</param>
    /// <param name="machineId">The target machine, or <see langword="null"/> to scope by the attempt.</param>
    /// <param name="attemptId">The ingest attempt, or <see langword="null"/> to scope by the machine.</param>
    /// <param name="chatId">The chat, or <see langword="null"/> to scope by the machine or the attempt.</param>
    /// <returns>The query over the machine drafts.</returns>
    public static IQueryable<MachineDraftGridItem> Query(
        IQueryable<MachineDraft> machineDrafts,
        IQueryable<Machine> machines,
        Guid companyId,
        Guid? machineId,
        Guid? attemptId,
        Guid? chatId
    )
    {
        var companyMachines = machines.Where(machine => machine.CompanyId == companyId);

        return machineDrafts
            .Where(draft => draft.CompanyId == companyId
                && (machineId == null || draft.TargetMachineId == machineId)
                && (attemptId == null || draft.Proposal.AttemptId == attemptId)
                && (chatId == null || draft.Proposal.ChatId == chatId))
            .Select(draft => new MachineDraftGridItem
            {
                Id = draft.Id,
                Kind = DraftKinds.Machine,
                State = draft.State,
                Proposal = new DraftProposalSummary
                {
                    Source = draft.Proposal.Source,
                    AttemptId = draft.Proposal.AttemptId,
                    CaseId = draft.Proposal.CaseId,
                    ChatId = draft.Proposal.ChatId,
                    RequestedByName = draft.Proposal.RequestedByName,
                },
                ProposedAt = draft.CreatedAt,
                SourceLocation = null,
                Label = draft.Name ?? draft.SerialNumber,
                RowVersion = draft.RowVersion,
                IsCreate = draft.TargetMachineId == null,
                MachineId = draft.TargetMachineId ?? draft.AppliedMachineId,
                MachineName = companyMachines
                        .Where(machine => machine.Id == (draft.TargetMachineId ?? draft.AppliedMachineId))
                        .Select(machine => machine.Name)
                        .FirstOrDefault()
                    ?? draft.Name,
            });
    }
}
