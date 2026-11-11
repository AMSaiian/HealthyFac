using HealthyFac.Auth.Application.Locales;

namespace HealthyFac.Auth.Application.Identities.IdentityAdmin;

/// <summary>
/// The admin operations of the identity provider that the auth service needs. Every method that changes state is safe
/// to repeat, so a handler that resumes after a stop continues from the state in the provider. Transient failures
/// (network errors, timeouts, and <c>5xx</c> answers) throw <see cref="HttpRequestException"/> and propagate for a
/// retry.
/// </summary>
public interface IIdentityAdmin
{
    /// <summary>
    /// Finds the organization of a company. The organization name is the company ID.
    /// </summary>
    /// <param name="companyId">The company ID.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to observe for cancellation requests.
    /// </param>
    /// <returns>The organization ID, or <see langword="null"/> when no organization has that name.</returns>
    Task<Guid?> FindOrganizationIdAsync(Guid companyId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the user that holds the employee ID in its <c>employee_id</c> attribute.
    /// </summary>
    /// <param name="employeeId">The employee ID.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to observe for cancellation requests.
    /// </param>
    /// <returns>The user ID, or <see langword="null"/> when no user holds the attribute.</returns>
    Task<Guid?> FindUserIdByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an enabled user with a verified email. The user has no password.
    /// </summary>
    /// <param name="identity">The data of the user.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to observe for cancellation requests.
    /// </param>
    /// <returns>The ID of the new user.</returns>
    /// <exception cref="IdentityConflictException">
    /// Thrown when a user with the same email exists already (see <see cref="IdentityAdminErrors.IdentityConflict"/>).
    /// </exception>
    Task<Guid> CreateUserAsync(NewIdentity identity, CancellationToken cancellationToken);

    /// <summary>
    /// Adds the user to the organization. A user that is a member already is not an error.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to observe for cancellation requests.
    /// </param>
    /// <returns>A task that completes when the user is a member.</returns>
    Task AddOrganizationMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Maps a realm role to the user. A role that is mapped already stays mapped once.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="roleName">The name of the realm role.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to observe for cancellation requests.
    /// </param>
    /// <returns>A task that completes when the user holds the role.</returns>
    /// <exception cref="RealmRoleNotFoundException">
    /// Thrown when the realm has no role with that name (see <see cref="IdentityAdminErrors.RealmRoleNotFound"/>).
    /// </exception>
    Task AssignRealmRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether the user has a credential of the type password.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to observe for cancellation requests.
    /// </param>
    /// <returns><see langword="true"/> when the user has set a password.</returns>
    Task<bool> HasPasswordAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Sends the user an email with a link to set the password.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to observe for cancellation requests.
    /// </param>
    /// <returns>A task that completes when the provider has accepted the email.</returns>
    Task SendSetPasswordEmailAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Disables the user, so the user cannot sign in again.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to observe for cancellation requests.
    /// </param>
    /// <returns>A task that completes when the user is disabled.</returns>
    Task DisableUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Ends every session of the user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to observe for cancellation requests.
    /// </param>
    /// <returns>A task that completes when the sessions are ended.</returns>
    Task LogoutUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the <c>locale</c> attribute of the user and nothing else: the provider keeps every other attribute and
    /// field. The same value twice changes nothing.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="locale">The locale.</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to observe for cancellation requests.
    /// </param>
    /// <returns>A task that completes when the attribute holds the locale.</returns>
    Task SetUserLocaleAsync(Guid userId, Locale locale, CancellationToken cancellationToken);
}
