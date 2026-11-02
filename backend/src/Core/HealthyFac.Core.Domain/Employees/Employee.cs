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

    public Guid Id { get; private set; } = Guid.CreateVersion7();
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

    /// <summary>
    /// The time of the last provisioning retry (UTC), or <see langword="null"/> until the first retry. Every retry
    /// sets it, so the employee row always changes and the concurrency check applies: a retry and a deactivation of
    /// the same employee cannot both succeed.
    /// </summary>
    public DateTime? ProvisioningRetriedAt { get; private set; }

    /// <summary>
    /// Whether the employee still waits for an identity: provisioning, or failed. A provisioning employee can retry,
    /// because the Auth inbox can give up on a message for good and leave the employee in this status.
    /// </summary>
    public bool CanRetryProvisioning
        => Status == EmployeeStatus.Provisioning || Status == EmployeeStatus.ProvisioningFailed;

    public bool CanDeactivate => Status != EmployeeStatus.Deactivated;

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
    /// Tells whether the employee is linked to <paramref name="identityUserId"/>. An employee without an identity
    /// is linked to nothing, so <see langword="null"/> never matches <see langword="null"/>. Without that, an
    /// employee who is still provisioning would count as the same person for an actor without a user ID.
    /// </summary>
    /// <param name="identityUserId">The Keycloak subject to compare, or <see langword="null"/> for none.</param>
    /// <returns><see langword="true"/> when the employee has an identity and it equals the argument.</returns>
    public bool IsLinkedTo(Guid? identityUserId) => IdentityUserId is not null && IdentityUserId == identityUserId;

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
        if (IdentityUserId is not null && !IsLinkedTo(identityUserId))
        {
            throw EmployeeErrors.IdentityAlreadyLinked(Id);
        }

        IdentityUserId = identityUserId;
    }

    /// <summary>
    /// Activates a provisioning employee and links the identity that the Auth service created. The use case that
    /// handles the <c>IdentityProvisioned</c> message calls it, and so does the development seed.
    /// Throws <see cref="InvalidOperationException"/> when the employee is not provisioning.
    /// </summary>
    /// <param name="identityUserId">The Keycloak subject of the identity that the Auth service created.</param>
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
    /// Starts identity provisioning again. The status becomes <see cref="EmployeeStatus.Provisioning"/>, and
    /// <see cref="ProvisioningRetriedAt"/> gets the time of the retry. Every retry changes the row, also for an
    /// employee who is provisioning already, so the concurrency check applies to it.
    /// Throws <see cref="InvalidOperationException"/> when the employee is active or deactivated.
    /// </summary>
    /// <param name="retriedAt">The time of the retry (UTC).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="CanRetryProvisioning"/> is <see langword="false"/>.
    /// </exception>
    public void RetryProvisioning(DateTime retriedAt)
    {
        if (!CanRetryProvisioning)
        {
            throw EmployeeErrors.InvalidStatusTransition(Id, Status, EmployeeStatus.Provisioning);
        }

        Status = EmployeeStatus.Provisioning;
        ProvisioningRetriedAt = retriedAt;
    }

    /// <summary>
    /// Deactivates the employee.
    /// Throws <see cref="InvalidOperationException"/> when the employee is already deactivated.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="CanDeactivate"/> is <see langword="false"/>.
    /// </exception>
    public void Deactivate()
    {
        if (!CanDeactivate)
        {
            throw EmployeeErrors.InvalidStatusTransition(Id, Status, EmployeeStatus.Deactivated);
        }

        Status = EmployeeStatus.Deactivated;
    }

    /// <summary>
    /// Finds the assigned department that matches <paramref name="department"/> by identifier, because a second
    /// query can load another instance of the same row.
    /// </summary>
    /// <param name="department">The department to find.</param>
    /// <returns>The assigned instance, or <see langword="null"/> when the department is not assigned.</returns>
    private Department? FindDepartment(Department department)
        => _departments.FirstOrDefault(assigned => assigned.Id == department.Id);

    private void EnsureStatus(EmployeeStatus requiredStatus, EmployeeStatus targetStatus)
    {
        if (Status != requiredStatus)
        {
            throw EmployeeErrors.InvalidStatusTransition(Id, Status, targetStatus);
        }
    }
}
