using HealthyFac.Shared.Web.Authentication;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

/// <summary>
/// The fixed values for every placeholder of the realm file, and the expectations of the realm tests.
/// </summary>
public static class RealmTestData
{
    public const string SupervisorEmail = "supervisor@healthyfac.local";
    public const string SupervisorPassword = "Sup3rvisor-Passw0rd!";
    public const string SpaUrl = "http://localhost:4200";
    public const string AuthClientSecret = "test-auth-client-secret";
    public const string CoreClientId = "healthyfac-core";
    public const string AuthClientId = "healthyfac-auth";
    public const string PermissionsClaim = KeycloakClaimTypes.Permissions;
    public const string SupervisorRoleName = "supervisor";
    public const string ClientIdClaim = "client_id";
    public const int AccessTokenLifespanSeconds = 300;
    public const string EmployeeIdAttribute = "employee_id";
    public const string AdminRole = "admin";

    public static readonly Guid CompanyId = Guid.Parse("0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid SupervisorIdentityId = Guid.Parse("0190f3a0-0000-7000-8000-000000000002");
    public static readonly Guid SupervisorEmployeeId = Guid.Parse("0190f3a0-0000-7000-8000-000000000003");

    public static IReadOnlyDictionary<string, string> Environment { get; } = new Dictionary<string, string>
    {
        ["HEALTHYFAC_SPA_URL"] = SpaUrl,
        ["HEALTHYFAC_TEST_COMPANY_ID"] = CompanyId.ToString(),
        ["HEALTHYFAC_TEST_SUPERVISOR_IDENTITY_ID"] = SupervisorIdentityId.ToString(),
        ["HEALTHYFAC_TEST_SUPERVISOR_EMPLOYEE_ID"] = SupervisorEmployeeId.ToString(),
        ["HEALTHYFAC_TEST_SUPERVISOR_EMAIL"] = SupervisorEmail,
        ["HEALTHYFAC_TEST_SUPERVISOR_PASSWORD"] = SupervisorPassword,
        ["KEYCLOAK_AUTH_CLIENT_SECRET"] = AuthClientSecret,
    };

    public static IReadOnlyList<string> RoleContainerClaims { get; } =
    [
        "resource_access",
        "realm_access",
    ];

    public static IReadOnlyList<string> RemovedServiceAccountClaims { get; } =
    [
        "clientHost",
        "clientAddress",
    ];

    public static IReadOnlyList<string> ExpectedPermissions { get; } =
    [
        "employees:invite",
        "employees:manage",
        "machines:write",
    ];
}
