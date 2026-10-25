using HealthyFac.Core.Domain.Departments;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Employees;

/// <summary>
/// Stages a new employee. The admin sets the email, the role, and the departments. The invited person fills in
/// the profile through the invite link, and <see cref="Accept"/> converts the complete invite into an
/// <see cref="Employee"/>. The invite stores only a random <see cref="TokenSalt"/>. The link token is a
/// signature over the invite ID and the salt with a key from configuration, so the database never holds a
/// usable token.
/// </summary>
public class EmployeeInvite : IEntity, IAuditable
{
    private readonly List<Department> _departments = [];

    /// <summary>
    /// Creates an open invite. The email is trimmed and converted to lower case, so uniqueness checks ignore
    /// case. Throws <see cref="InvalidOperationException"/> when <paramref name="departments"/> is empty or holds
    /// a department of another company.
    /// </summary>
    /// <param name="companyId">The company of the admin who invites.</param>
    /// <param name="email">The email address of the invited person.</param>
    /// <param name="roleId">The role that the employee gets.</param>
    /// <param name="departments">The departments that the employee joins. At least one is required.</param>
    /// <param name="tokenSalt">The random salt that the link token signs.</param>
    /// <param name="expiresAt">The UTC time after which the link stops working.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="departments"/> is empty, because an employee without a department gets no
    /// tasks, or when a department belongs to a company other than <paramref name="companyId"/>.
    /// </exception>
    public EmployeeInvite(
        Guid companyId,
        string email,
        Guid roleId,
        IReadOnlyCollection<Department> departments,
        byte[] tokenSalt,
        DateTime expiresAt
    )
        : this(companyId, email.Trim().ToLowerInvariant(), roleId, tokenSalt, expiresAt)
    {
        if (departments.Count == 0)
        {
            throw EmployeeErrors.InviteWithoutDepartments();
        }

        var foreignDepartment = departments.FirstOrDefault(department => department.CompanyId != companyId);
        if (foreignDepartment is not null)
        {
            throw EmployeeErrors.InviteDepartmentOfOtherCompany(foreignDepartment.Id);
        }

        _departments.AddRange(departments);
    }

    private EmployeeInvite(Guid companyId, string email, Guid roleId, byte[] tokenSalt, DateTime expiresAt)
    {
        CompanyId = companyId;
        Email = email;
        RoleId = roleId;
        TokenSalt = tokenSalt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid CompanyId { get; private set; }
    public string Email { get; private set; }
    public Guid RoleId { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public byte[] TokenSalt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    public IReadOnlyCollection<Department> Departments => _departments;

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public bool CanAccept(DateTime now) => AcceptedAt is null && now < ExpiresAt;

    /// <summary>
    /// Stores the profile fields that the invited person entered. Either field can stay empty until the
    /// person completes the profile. Throws <see cref="InvalidOperationException"/> after acceptance.
    /// </summary>
    /// <param name="firstName">The first name, or <see langword="null"/> when not entered yet.</param>
    /// <param name="lastName">The last name, or <see langword="null"/> when not entered yet.</param>
    /// <exception cref="InvalidOperationException">Thrown when the invite is already accepted.</exception>
    public void UpdateProfile(string? firstName, string? lastName)
    {
        EnsureNotAccepted();
        FirstName = firstName;
        LastName = lastName;
    }

    /// <summary>
    /// Converts the complete invite into an employee with the status
    /// <see cref="EmployeeStatus.Provisioning"/>. The caller adds the employee and saves both in one
    /// transaction. Throws <see cref="InvalidOperationException"/> when the invite cannot be accepted.
    /// </summary>
    /// <param name="acceptedAt">The current UTC time.</param>
    /// <returns>The new employee with the email, the role, the departments, and the profile of the invite.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the invite is already accepted, when it is expired at <paramref name="acceptedAt"/>, or when
    /// the first name or the last name is missing.
    /// </exception>
    public Employee Accept(DateTime acceptedAt)
    {
        EnsureNotAccepted();

        if (acceptedAt >= ExpiresAt)
        {
            throw EmployeeErrors.InviteExpired(Id);
        }

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            throw EmployeeErrors.InviteProfileIncomplete(Id);
        }

        AcceptedAt = acceptedAt;

        return new Employee(CompanyId, Email, FirstName, LastName, RoleId, _departments);
    }

    /// <summary>
    /// Replaces the salt and the expiry for a resend. Every earlier link stops working, because its signature
    /// covers the old salt. Throws <see cref="InvalidOperationException"/> after acceptance.
    /// </summary>
    /// <param name="tokenSalt">The new random salt.</param>
    /// <param name="expiresAt">The new UTC expiry time.</param>
    /// <exception cref="InvalidOperationException">Thrown when the invite is already accepted.</exception>
    public void Renew(byte[] tokenSalt, DateTime expiresAt)
    {
        EnsureNotAccepted();
        TokenSalt = tokenSalt;
        ExpiresAt = expiresAt;
    }

    private void EnsureNotAccepted()
    {
        if (AcceptedAt is not null)
        {
            throw EmployeeErrors.InviteAlreadyAccepted(Id);
        }
    }
}
