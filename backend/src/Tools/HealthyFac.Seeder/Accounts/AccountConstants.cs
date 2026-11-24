namespace HealthyFac.Seeder.Accounts;

/// <summary>Defines the Keycloak admin paths and names that the seed accounts use beyond the Auth routes.</summary>
public static class AccountConstants
{
    public const string OrganizationsPath = "admin/realms/{0}/organizations";
    public const string OrganizationPath = "admin/realms/{0}/organizations/{1}";
    public const string ResetPasswordSuffix = "reset-password";
    public const string PasswordCredentialType = "password";
    public const string OrganizationNameFormat = "D";
    public const string MaxQueryParameter = "max";

    /// <summary>The members that one delete round reads. Keycloak returns 10 without a limit.</summary>
    public const int MemberPageSize = 100;
}
