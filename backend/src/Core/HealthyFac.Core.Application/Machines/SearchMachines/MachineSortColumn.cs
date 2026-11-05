using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Application.Machines.SearchMachines;

/// <summary>
/// The sortable columns of the machine grid. The columns that hold an enumeration (state) or a name that the grid
/// resolves in the browser (department) have no member, so the type blocks sorting on them.
/// </summary>
public sealed class MachineSortColumn : Enumeration<MachineSortColumn>
{
    public static readonly MachineSortColumn Name = new(value: "name");
    public static readonly MachineSortColumn SerialNumber = new(value: "serialNumber");
    public static readonly MachineSortColumn CreatedAt = new(value: "createdAt");
    public static readonly MachineSortColumn UpdatedAt = new(value: "updatedAt");

    private MachineSortColumn(string value)
        : base(value)
    {
    }
}
