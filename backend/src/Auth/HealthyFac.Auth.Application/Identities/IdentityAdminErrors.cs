namespace HealthyFac.Auth.Application.Identities;

/// <summary>
/// Defines the exceptions of the identity provider admin client. The texts never hold an email or a name.
/// </summary>
public static class IdentityAdminErrors
{
    public static IdentityConflictException IdentityConflict(Guid employeeId) =>
        new($"The identity provider already holds a user with the email of employee '{employeeId}'.");

    public static RealmRoleNotFoundException RealmRoleNotFound(string roleName) =>
        new(
            $"The identity provider has no realm role '{roleName}'. An admin must add the role, and a supervisor "
            + "must then retry provisioning.");

    public static InvalidOperationException ResponseEmpty() =>
        new("The identity provider answered success with an empty body.");

    public static InvalidOperationException CreatedUserIdMissing() =>
        new("The identity provider created the user but the response has no valid location header.");
}
