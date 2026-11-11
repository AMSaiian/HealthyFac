namespace HealthyFac.Auth.Infrastructure.Keycloak;

/// <summary>
/// Defines the names and values of the Keycloak admin API.
/// </summary>
public static class KeycloakAdminConstants
{
    public const int DefaultSetPasswordLifespanSeconds = 86400;

    /// <summary>
    /// The limit of one admin call. A hanging server then fails the attempt, and the inbox retries, instead of
    /// holding the processor for the default 100 seconds.
    /// </summary>
    public const int RequestTimeoutSeconds = 30;

    /// <summary>The user attribute that holds the employee ID. The user profile of the realm declares it.</summary>
    public const string EmployeeIdAttribute = "employee_id";

    /// <summary>The user attribute that holds the locale. The user profile of the realm declares it.</summary>
    public const string LocaleAttribute = "locale";

    public const string AttributesProperty = "attributes";

    public const string EnabledProperty = "enabled";

    /// <summary>The credential type of a password.</summary>
    public const string PasswordCredentialType = "password";

    /// <summary>The required action that asks the user to choose a password.</summary>
    public const string UpdatePasswordAction = "UPDATE_PASSWORD";

    /// <summary>The format of a Company ID in the name of its organization (name = alias).</summary>
    public const string OrganizationNameFormat = "D";

    public const char PathSeparator = '/';
}
