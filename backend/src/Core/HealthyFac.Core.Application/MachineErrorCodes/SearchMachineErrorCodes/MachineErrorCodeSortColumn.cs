using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Application.MachineErrorCodes.SearchMachineErrorCodes;

/// <summary>
/// The sortable columns of the error code grid.
/// </summary>
public sealed class MachineErrorCodeSortColumn : Enumeration<MachineErrorCodeSortColumn>
{
    public static readonly MachineErrorCodeSortColumn Code = new("code");
    public static readonly MachineErrorCodeSortColumn CreatedAt = new("createdAt");
    public static readonly MachineErrorCodeSortColumn UpdatedAt = new("updatedAt");

    private MachineErrorCodeSortColumn(string value)
        : base(value)
    {
    }
}
