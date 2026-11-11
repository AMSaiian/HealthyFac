using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Employees;

/// <summary>
/// Represents a maintenance worker or a supervisor of one company. Keycloak owns the credentials. This entity
/// is the business mirror of the identity, so task lists and schedules can show and filter people without a
/// call to Keycloak. Only <see cref="EmployeeInvite.Accept"/> creates an employee.
/// </summary>
public class Employee : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    internal Employee(Guid companyId, string email, string firstName, string lastName, Guid roleId)
    {
        CompanyId = companyId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        RoleId = roleId;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public uint RowVersion { get; private set; }
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

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

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

    private void EnsureStatus(EmployeeStatus requiredStatus, EmployeeStatus targetStatus)
    {
        if (Status != requiredStatus)
        {
            throw EmployeeErrors.InvalidStatusTransition(Id, Status, targetStatus);
        }
    }
}
