using System.Linq.Expressions;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Defines the pending drafts of a machine for queries. The projection and the grid filter share it, so the rule has
/// one definition. A pending machine draft counts when it targets the machine. The metric and status drafts live in
/// Telemetry; the Gateway sends the machines that have pending ones to the grid filter.
/// </summary>
public static class MachinePendingDrafts
{
    /// <summary>
    /// Counts the pending machine drafts that target the machine. EF Core translates it to a correlated subquery.
    /// </summary>
    /// <param name="dbContext">Provides the draft sets.</param>
    /// <returns>The count selector for a projection.</returns>
    public static Expression<Func<Machine, int>> CountSelector(ICoreDbContext dbContext) =>
        machine => dbContext.MachineDrafts
            .Count(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending);

    /// <summary>
    /// Builds the predicate "the machine has at least one pending draft", or its negation. A machine has pending
    /// drafts when a pending machine draft targets it, or when Telemetry reports it.
    /// </summary>
    /// <param name="dbContext">Provides the draft sets.</param>
    /// <param name="hasPendingDrafts"><see langword="true"/> for machines with drafts, else machines without.</param>
    /// <param name="pendingDefinitionDraftMachineIds">
    /// The machines that have pending metric or status drafts in Telemetry, or <see langword="null"/> for none.
    /// </param>
    /// <returns>The predicate for a <c>Where</c> call.</returns>
    public static Expression<Func<Machine, bool>> Predicate(
        ICoreDbContext dbContext,
        bool hasPendingDrafts,
        IReadOnlyList<Guid>? pendingDefinitionDraftMachineIds
    )
    {
        var reportedIds = pendingDefinitionDraftMachineIds ?? [];

        return hasPendingDrafts
            ? machine => dbContext.MachineDrafts
                    .Any(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending)
                || reportedIds.Contains(machine.Id)
            : machine => !dbContext.MachineDrafts
                    .Any(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending)
                && !reportedIds.Contains(machine.Id);
    }
}
