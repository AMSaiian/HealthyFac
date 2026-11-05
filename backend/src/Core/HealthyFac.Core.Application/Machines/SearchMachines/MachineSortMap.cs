using System.Collections.Frozen;
using System.Linq.Expressions;
using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Core.Application.Machines.SearchMachines;

/// <summary>
/// Maps each sort column of the machine grid to its key expression.
/// </summary>
public static class MachineSortMap
{
    public static readonly FrozenDictionary<MachineSortColumn, Expression<Func<Machine, dynamic>>> Instance =
        new Dictionary<MachineSortColumn, Expression<Func<Machine, dynamic>>>
        {
            [MachineSortColumn.Name] = machine => machine.Name,
            [MachineSortColumn.SerialNumber] = machine => machine.SerialNumber,
            [MachineSortColumn.CreatedAt] = machine => machine.CreatedAt,
            [MachineSortColumn.UpdatedAt] = machine => machine.UpdatedAt!,
        }.ToFrozenDictionary();
}
