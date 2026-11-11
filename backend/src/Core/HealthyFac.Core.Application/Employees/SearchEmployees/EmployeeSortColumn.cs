using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Application.Employees.SearchEmployees;

/// <summary>
/// The sortable columns of the employee grid. The columns that hold an enumeration (role, status) have no member, so
/// the type blocks sorting on them.
/// </summary>
public sealed class EmployeeSortColumn : Enumeration<EmployeeSortColumn>
{
    public static readonly EmployeeSortColumn Name = new(value: "name");
    public static readonly EmployeeSortColumn Email = new(value: "email");
    public static readonly EmployeeSortColumn CreatedAt = new(value: "createdAt");

    private EmployeeSortColumn(string value)
        : base(value)
    {
    }
}
