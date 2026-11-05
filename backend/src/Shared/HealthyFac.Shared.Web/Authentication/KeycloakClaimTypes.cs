namespace HealthyFac.Shared.Web.Authentication;

/// <summary>
/// Defines the claim types that Keycloak puts in an access token.
/// </summary>
public static class KeycloakClaimTypes
{
    public const string Organization = "organization";
    public const string EmployeeId = "employee_id";
    public const string Permissions = "permissions";
    public const string DepartmentIds = "department_ids";

    /// <summary>The client that asked for the token. A token of a service account has it.</summary>
    public const string ClientId = "client_id";
}
