using System.Linq.Expressions;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Defines the pending drafts of a machine for queries. The projection and the grid filter share it, so the rule has
/// one definition. A pending draft counts when it targets the machine. The children of a pending create draft do not
/// count, because they target the draft and not a machine.
/// </summary>
public static class MachinePendingDrafts
{
    /// <summary>
    /// Counts the pending machine, metric, and status drafts that target the machine. EF Core translates it to
    /// correlated subqueries.
    /// </summary>
    /// <param name="dbContext">Provides the draft sets.</param>
    /// <returns>The count selector for a projection.</returns>
    public static Expression<Func<Machine, int>> CountSelector(ICoreDbContext dbContext) =>
        machine => dbContext.MachineDrafts
                .Count(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending)
            + dbContext.MetricDefinitionDrafts
                .Count(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending)
            + dbContext.StatusDefinitionDrafts
                .Count(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending);

    /// <summary>
    /// Builds the predicate "the machine has at least one pending draft", or its negation.
    /// </summary>
    /// <param name="dbContext">Provides the draft sets.</param>
    /// <param name="hasPendingDrafts"><see langword="true"/> for machines with drafts, else machines without.</param>
    /// <returns>The predicate for a <c>Where</c> call.</returns>
    public static Expression<Func<Machine, bool>> Predicate(ICoreDbContext dbContext, bool hasPendingDrafts) =>
        hasPendingDrafts
            ? machine => dbContext.MachineDrafts
                    .Any(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending)
                || dbContext.MetricDefinitionDrafts
                    .Any(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending)
                || dbContext.StatusDefinitionDrafts
                    .Any(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending)
            : machine => !dbContext.MachineDrafts
                    .Any(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending)
                && !dbContext.MetricDefinitionDrafts
                    .Any(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending)
                && !dbContext.StatusDefinitionDrafts
                    .Any(draft => draft.TargetMachineId == machine.Id && draft.State == DraftState.Pending);
}
