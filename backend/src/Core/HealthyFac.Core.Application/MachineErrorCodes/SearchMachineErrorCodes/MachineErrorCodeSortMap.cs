using System.Collections.Frozen;
using System.Linq.Expressions;
using HealthyFac.Core.Domain.MachineErrorCodes;

namespace HealthyFac.Core.Application.MachineErrorCodes.SearchMachineErrorCodes;

/// <summary>
/// Maps each sort column of the error code grid to its key expression.
/// </summary>
public static class MachineErrorCodeSortMap
{
    public static readonly FrozenDictionary<MachineErrorCodeSortColumn, Expression<Func<MachineErrorCode, dynamic>>>
        Instance =
            new Dictionary<MachineErrorCodeSortColumn, Expression<Func<MachineErrorCode, dynamic>>>
            {
                [MachineErrorCodeSortColumn.Code] = errorCode => errorCode.Code,
                [MachineErrorCodeSortColumn.CreatedAt] = errorCode => errorCode.CreatedAt,
                [MachineErrorCodeSortColumn.UpdatedAt] = errorCode => errorCode.UpdatedAt!,
            }.ToFrozenDictionary();
}
