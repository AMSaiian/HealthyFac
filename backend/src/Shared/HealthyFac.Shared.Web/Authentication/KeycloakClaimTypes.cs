namespace HealthyFac.Shared.Web.Authentication;

/// <summary>
/// Defines the claim types that Keycloak puts in an access token.
/// </summary>
public static class KeycloakClaimTypes
{
    public const string Organization = "organization";
    public const string EmployeeId = "employee_id";
    public const string Permissions = "permissions";
}
