using HealthyFac.Core.Domain.Departments;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Employees;

/// <summary>
/// Represents a maintenance worker or a supervisor of one company. Keycloak owns the credentials. This entity
/// is the business mirror of the identity, so task lists and schedules can show and filter people without a
/// call to Keycloak. Only <see cref="EmployeeInvite.Accept"/> creates an employee.
/// </summary>
public class Employee : IEntity, IAuditable
{
    private readonly List<Department> _departments = [];

    internal Employee(
        Guid companyId,
        string email,
        string firstName,
        string lastName,
        Guid roleId,
        IEnumerable<Department> departments
    )
        : this(companyId, email, firstName, lastName, roleId)
    {
        _departments.AddRange(departments);
    }

    private Employee(Guid companyId, string email, string firstName, string lastName, Guid roleId)
    {
        CompanyId = companyId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        RoleId = roleId;
    }

    public Guid Id { get; private set; } = Guid.Empty;
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// The Keycloak subject (<c>sub</c>) of the employee, or <see langword="null"/> until the Auth service
    /// provisions the identity.
    /// </summary>
    public Guid? IdentityUserId { get; private set; }

    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Guid RoleId { get; private set; }
    public EmployeeStatus Status { get; private set; } = EmployeeStatus.Provisioning;

    public IReadOnlyCollection<Department> Departments => _departments;

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    /// <summary>
    /// Adds the employee to <paramref name="department"/>. Assigning a department again changes nothing. Load
    /// <see cref="Departments"/> before the call, or the duplicate check sees an empty list.
    /// Throws <see cref="InvalidOperationException"/> when the department belongs to another company.
    /// </summary>
    /// <param name="department">The department of the same company.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="department"/> belongs to a company other than the company of the employee.
    /// </exception>
    public void AssignDepartment(Department department)
    {
        if (department.CompanyId != CompanyId)
        {
            throw EmployeeErrors.DepartmentOfOtherCompany(Id, department.Id);
        }

        if (FindDepartment(department) is not null)
        {
            return;
        }

        _departments.Add(department);
    }

    /// <summary>
    /// Removes the employee from <paramref name="department"/>. Removing an unassigned department changes nothing.
    /// Load <see cref="Departments"/> before the call, or the last-department check sees an empty list.
    /// Throws <see cref="InvalidOperationException"/> when it is the last department of the employee.
    /// </summary>
    /// <param name="department">The department to leave.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="department"/> is the only department, because an employee without a
    /// department gets no tasks.
    /// </exception>
    public void UnassignDepartment(Department department)
    {
        var assignedDepartment = FindDepartment(department);
        if (assignedDepartment is null)
        {
            return;
        }

        if (_departments.Count == 1)
        {
            throw EmployeeErrors.LastDepartment(Id);
        }

        _departments.Remove(assignedDepartment);
    }

    /// <summary>
    /// Links the employee to the Keycloak identity that the Auth service created. Linking the same identity
    /// again changes nothing, so a duplicate message is harmless.
    /// Throws <see cref="InvalidOperationException"/> when another identity is already linked.
    /// </summary>
    /// <param name="identityUserId">The Keycloak subject of the employee.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the employee is already linked to an identity other than <paramref name="identityUserId"/>.
    /// </exception>
    public void LinkIdentity(Guid identityUserId)
    {
        if (IdentityUserId is not null && IdentityUserId != identityUserId)
        {
            throw EmployeeErrors.IdentityAlreadyLinked(Id);
        }

        IdentityUserId = identityUserId;
    }

    /// <summary>
    /// Activates the employee on the first authenticated request. It also links the identity, because the
    /// first sign-in can arrive before the Auth service message that links it.
    /// Throws <see cref="InvalidOperationException"/> when the employee is not provisioning.
    /// </summary>
    /// <param name="identityUserId">The Keycloak subject from the access token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the status is not <see cref="EmployeeStatus.Provisioning"/>.
    /// </exception>
    public void Activate(Guid identityUserId)
    {
        EnsureStatus(EmployeeStatus.Provisioning, EmployeeStatus.Active);
        LinkIdentity(identityUserId);
        Status = EmployeeStatus.Active;
    }

    /// <summary>
    /// Records that the Auth service could not create the identity.
    /// Throws <see cref="InvalidOperationException"/> when the employee is not provisioning.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the status is not <see cref="EmployeeStatus.Provisioning"/>.
    /// </exception>
    public void MarkProvisioningFailed()
    {
        EnsureStatus(EmployeeStatus.Provisioning, EmployeeStatus.ProvisioningFailed);
        Status = EmployeeStatus.ProvisioningFailed;
    }

    /// <summary>
    /// Starts identity provisioning again after a failure.
    /// Throws <see cref="InvalidOperationException"/> when the last provisioning did not fail.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the status is not <see cref="EmployeeStatus.ProvisioningFailed"/>.
    /// </exception>
    public void RetryProvisioning()
    {
        EnsureStatus(EmployeeStatus.ProvisioningFailed, EmployeeStatus.Provisioning);
        Status = EmployeeStatus.Provisioning;
    }

    /// <summary>
    /// Deactivates the employee.
    /// Throws <see cref="InvalidOperationException"/> when the employee is already deactivated.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the status is already <see cref="EmployeeStatus.Deactivated"/>.
    /// </exception>
    public void Deactivate()
    {
        if (Status == EmployeeStatus.Deactivated)
        {
            throw EmployeeErrors.InvalidStatusTransition(Id, Status, EmployeeStatus.Deactivated);
        }

        Status = EmployeeStatus.Deactivated;
    }

    /// <summary>
    /// Finds the assigned department that matches <paramref name="department"/>. A saved department matches by
    /// identifier, because a second query can load another instance of the same row. An unsaved department has
    /// no identifier yet, so it matches only the same instance.
    /// </summary>
    /// <param name="department">The department to find.</param>
    /// <returns>The assigned instance, or <see langword="null"/> when the department is not assigned.</returns>
    private Department? FindDepartment(Department department)
        => _departments.FirstOrDefault(assigned => ReferenceEquals(assigned, department)
            || (assigned.Id != Guid.Empty && assigned.Id == department.Id));

    private void EnsureStatus(EmployeeStatus requiredStatus, EmployeeStatus targetStatus)
    {
        if (Status != requiredStatus)
        {
            throw EmployeeErrors.InvalidStatusTransition(Id, Status, targetStatus);
        }
    }
}
