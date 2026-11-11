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
    public const string TelemetryClientSecret = "test-telemetry-client-secret";
    public const string DocumentClientSecret = "test-document-client-secret";
    public const string InsightsClientSecret = "test-insights-client-secret";
    public const string PublicAuthUrl = "https://auth.healthyfac.test";
    public const string InternalHost = "keycloak:8080";
    public const string CoreClientId = "healthyfac-core";
    public const string TelemetryClientId = "healthyfac-telemetry";
    public const string DocumentClientId = "healthyfac-document";
    public const string InsightsClientId = "healthyfac-insights";
    public const string AuthClientId = "healthyfac-auth";
    public const string PermissionsClaim = KeycloakClaimTypes.Permissions;
    public const string SupervisorRoleName = "supervisor";
    public const string ClientIdClaim = "client_id";
    public const int AccessTokenLifespanSeconds = 300;
    public const string EmployeeIdAttribute = "employee_id";
    public const string AdminRole = "admin";
    public const string IdListClaimSuffix = "_ids";
    public const string WorkerRoleName = "worker";

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid SupervisorIdentityId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000002");
    public static readonly Guid SupervisorEmployeeId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000003");

    public static IReadOnlyDictionary<string, string> Environment { get; } = new Dictionary<string, string>
    {
        [key: "HEALTHYFAC_SPA_URL"] = SpaUrl,
        [key: "HEALTHYFAC_TEST_COMPANY_ID"] = CompanyId.ToString(),
        [key: "HEALTHYFAC_TEST_SUPERVISOR_IDENTITY_ID"] = SupervisorIdentityId.ToString(),
        [key: "HEALTHYFAC_TEST_SUPERVISOR_EMPLOYEE_ID"] = SupervisorEmployeeId.ToString(),
        [key: "HEALTHYFAC_TEST_SUPERVISOR_EMAIL"] = SupervisorEmail,
        [key: "HEALTHYFAC_TEST_SUPERVISOR_PASSWORD"] = SupervisorPassword,
        [key: "KEYCLOAK_AUTH_CLIENT_SECRET"] = AuthClientSecret,
        [key: "KEYCLOAK_CORE_CLIENT_SECRET"] = CoreClientSecret,
        [key: "KEYCLOAK_TELEMETRY_CLIENT_SECRET"] = TelemetryClientSecret,
        [key: "KEYCLOAK_DOCUMENT_CLIENT_SECRET"] = DocumentClientSecret,
        [key: "KEYCLOAK_INSIGHTS_CLIENT_SECRET"] = InsightsClientSecret,
        [key: "KC_HOSTNAME"] = PublicAuthUrl,
        [key: "KC_HOSTNAME_BACKCHANNEL_DYNAMIC"] = "true",
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
    ];

    public static IReadOnlyList<string> CorePermissions { get; } =
    [
        "employees:invite",
        "employees:manage",
        "machines:read",
        "machines:write",
        "machines:delete",
        "drafts:read",
        "drafts:write",
    ];

    public static IReadOnlyList<string> TelemetryPermissions { get; } =
    [
        "metric-definitions:read",
        "metric-definitions:write",
        "status-definitions:read",
        "status-definitions:write",
        "status-definitions:delete",
    ];

    public static IReadOnlyList<string> DocumentPermissions { get; } =
    [
        "files:read",
        "files:write",
        "files:delete",
        "company-files:write",
        "company-files:delete",
    ];

    public static IReadOnlyList<string> InsightsPermissions { get; } =
    [
        "ingest:read",
        "ingest:write",
    ];

    /// <summary>The permissions of a token that the web app gets: the permissions of every API.</summary>
    public static IReadOnlyList<string> ExpectedPermissions { get; } =
    [
        .. CorePermissions,
        .. TelemetryPermissions,
        .. DocumentPermissions,
        .. InsightsPermissions,
    ];

    public static IReadOnlyList<string> MachinePermissions { get; } =
    [
        "machines:read",
        "machines:write",
        "machines:delete",
    ];
}
