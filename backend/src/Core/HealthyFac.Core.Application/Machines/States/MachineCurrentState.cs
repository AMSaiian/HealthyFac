using System.Linq.Expressions;
using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Defines the current state of a machine for queries. The projection and the grid filter share it, so the rule has
/// one definition.
/// </summary>
public static class MachineCurrentState
{
    /// <summary>
    /// The state of the history row with the latest <see cref="MachineStateHistory.StartedAt"/>. The soft-delete
    /// query filter applies, so a deleted row never counts. EF Core translates it to a correlated subquery.
    /// </summary>
    public static readonly Expression<Func<Machine, MachineState>> Selector = machine => machine.StateHistory
        .OrderByDescending(row => row.StartedAt)
        .Select(row => row.State)
        .First();
}
