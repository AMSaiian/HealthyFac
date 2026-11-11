using System.Collections.Frozen;
using System.Linq.Expressions;
using HealthyFac.Core.Domain.Employees;

namespace HealthyFac.Core.Application.Employees.SearchEmployeeInvites;

/// <summary>
/// Maps each sort column of the pending invite grid to its key expression.
/// </summary>
public static class EmployeeInviteSortMap
{
    public static readonly FrozenDictionary<EmployeeInviteSortColumn, Expression<Func<EmployeeInvite, dynamic>>>
        Instance = new Dictionary<EmployeeInviteSortColumn, Expression<Func<EmployeeInvite, dynamic>>>
        {
            [EmployeeInviteSortColumn.ExpiresAt] = invite => invite.ExpiresAt,
            [EmployeeInviteSortColumn.Email] = invite => invite.Email,
        }.ToFrozenDictionary();
}
