namespace HealthyFac.Seeder.Accounts;

/// <summary>
/// Defines the error messages of the seed accounts.
/// </summary>
public static class AccountErrors
{
    public const string PasswordEqualsUsername = "The password must differ from the username.";

    public const string OrganizationIdMissing = "Keycloak returned no ID for the created organization.";

    public static string PasswordTooShort(int minLength) =>
        $"The password must have at least {minLength} characters.";

    public static string OrganizationNotFound(Guid companyId) =>
        $"Keycloak holds no organization for the company {companyId}.";
}
