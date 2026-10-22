namespace HealthyFac.Core.Domain.Employees;

/// <summary>
/// Defines the exceptions that employees and employee invites throw.
/// </summary>
public static class EmployeeErrors
{
    public static InvalidOperationException InviteWithoutDepartments() =>
        new("An employee invite needs at least one department.");

    public static InvalidOperationException InviteDepartmentOfOtherCompany(Guid departmentId) =>
        new($"Department '{departmentId}' belongs to another company than the employee invite.");

    public static InvalidOperationException InviteAlreadyAccepted(Guid inviteId) =>
        new($"Employee invite '{inviteId}' is already accepted.");

    public static InvalidOperationException InviteExpired(Guid inviteId) =>
        new($"Employee invite '{inviteId}' is expired.");

    public static InvalidOperationException InviteProfileIncomplete(Guid inviteId) =>
        new($"Employee invite '{inviteId}' has no first name or no last name.");

    public static InvalidOperationException DepartmentOfOtherCompany(Guid employeeId, Guid departmentId) =>
        new($"Department '{departmentId}' belongs to another company than employee '{employeeId}'.");

    public static InvalidOperationException LastDepartment(Guid employeeId) =>
        new($"Employee '{employeeId}' cannot leave the last department.");

    public static InvalidOperationException IdentityAlreadyLinked(Guid employeeId) =>
        new($"Employee '{employeeId}' is already linked to another identity.");

    public static InvalidOperationException InvalidStatusTransition(
        Guid employeeId,
        EmployeeStatus currentStatus,
        EmployeeStatus targetStatus
    ) =>
        new($"Employee '{employeeId}' cannot change status from '{currentStatus.Value}' to '{targetStatus.Value}'.");
}
