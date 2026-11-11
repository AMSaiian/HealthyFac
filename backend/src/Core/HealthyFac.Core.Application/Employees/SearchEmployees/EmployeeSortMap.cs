using System.Collections.Frozen;
using System.Linq.Expressions;
using HealthyFac.Core.Domain.Employees;

namespace HealthyFac.Core.Application.Employees.SearchEmployees;

/// <summary>
/// Maps each sort column of the employee grid to its key expression.
/// </summary>
public static class EmployeeSortMap
{
    public static readonly FrozenDictionary<EmployeeSortColumn, Expression<Func<Employee, dynamic>>> Instance =
        new Dictionary<EmployeeSortColumn, Expression<Func<Employee, dynamic>>>
        {
            [EmployeeSortColumn.Name] = employee => employee.FirstName + " " + employee.LastName,
            [EmployeeSortColumn.Email] = employee => employee.Email,
            [EmployeeSortColumn.CreatedAt] = employee => employee.CreatedAt,
        }.ToFrozenDictionary();
}
