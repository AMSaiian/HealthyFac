using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Application.Employees.SearchEmployeeInvites;

/// <summary>
/// The sortable columns of the pending invite grid.
/// </summary>
public sealed class EmployeeInviteSortColumn : Enumeration<EmployeeInviteSortColumn>
{
    public static readonly EmployeeInviteSortColumn ExpiresAt = new(value: "expiresAt");
    public static readonly EmployeeInviteSortColumn Email = new(value: "email");

    private EmployeeInviteSortColumn(string value)
        : base(value)
    {
    }
}
