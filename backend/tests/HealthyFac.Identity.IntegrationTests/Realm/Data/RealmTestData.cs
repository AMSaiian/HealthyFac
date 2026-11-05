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
    public const string CoreClientSecret = "test-core-client-secret";
    public const string PublicAuthUrl = "https://auth.healthyfac.test";
    public const string InternalHost = "keycloak:8080";
    public const string CoreClientId = "healthyfac-core";
    public const string AuthClientId = "healthyfac-auth";
    public const string PermissionsClaim = KeycloakClaimTypes.Permissions;
    public const string SupervisorRoleName = "supervisor";
    public const string ClientIdClaim = "client_id";
    public const int AccessTokenLifespanSeconds = 300;
    public const string EmployeeIdAttribute = "employee_id";
    public const string AdminRole = "admin";
    public const string DepartmentIdsClaim = KeycloakClaimTypes.DepartmentIds;
    public const string WorkerRoleName = "worker";

    public static readonly Guid CompanyId = Guid.Parse("0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid SupervisorIdentityId = Guid.Parse("0190f3a0-0000-7000-8000-000000000002");
    public static readonly Guid DepartmentAId = Guid.Parse("0190f3a0-0000-7000-8000-0000000000a1");
    public static readonly Guid DepartmentBId = Guid.Parse("0190f3a0-0000-7000-8000-0000000000b1");
    public static readonly Guid SupervisorEmployeeId = Guid.Parse("0190f3a0-0000-7000-8000-000000000003");

    public static IReadOnlyDictionary<string, string> Environment { get; } = new Dictionary<string, string>
    {
        ["HEALTHYFAC_SPA_URL"] = SpaUrl,
        ["HEALTHYFAC_TEST_COMPANY_ID"] = CompanyId.ToString(),
        ["HEALTHYFAC_TEST_SUPERVISOR_IDENTITY_ID"] = SupervisorIdentityId.ToString(),
        ["HEALTHYFAC_TEST_SUPERVISOR_EMPLOYEE_ID"] = SupervisorEmployeeId.ToString(),
        ["HEALTHYFAC_TEST_DEPARTMENT_A_ID"] = DepartmentAId.ToString(),
        ["HEALTHYFAC_TEST_DEPARTMENT_B_ID"] = DepartmentBId.ToString(),
        ["HEALTHYFAC_TEST_SUPERVISOR_EMAIL"] = SupervisorEmail,
        ["HEALTHYFAC_TEST_SUPERVISOR_PASSWORD"] = SupervisorPassword,
        ["KEYCLOAK_AUTH_CLIENT_SECRET"] = AuthClientSecret,
        ["KEYCLOAK_CORE_CLIENT_SECRET"] = CoreClientSecret,
        ["KC_HOSTNAME"] = PublicAuthUrl,
        ["KC_HOSTNAME_BACKCHANNEL_DYNAMIC"] = "true",
    };

    public static string PublicIssuer => $"{PublicAuthUrl}/realms/healthyfac";

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

    public static IReadOnlyList<string> UserOnlyClaims { get; } =
    [
        "organization",
        EmployeeIdAttribute,
        DepartmentIdsClaim,
    ];

    public static IReadOnlyList<string> ExpectedPermissions { get; } =
    [
        "employees:invite",
        "employees:manage",
        "machines:read",
        "machines:write",
        "machines:delete",
    ];

    public static IReadOnlyList<string> MachinePermissions { get; } =
    [
        "machines:read",
        "machines:write",
        "machines:delete",
    ];
}
