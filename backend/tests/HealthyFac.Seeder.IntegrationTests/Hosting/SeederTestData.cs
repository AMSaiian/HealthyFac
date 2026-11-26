using HealthyFac.Shared.Application.CurrentUser.Actors;

namespace HealthyFac.Seeder.IntegrationTests.Hosting;

public static class SeederTestData
{
    public const string RabbitMqUnused = "amqp://guest:guest@localhost:1";
    public const string IdEncryptionKey = "dGVzdC1pZC1lbmNyeXB0aW9uLWtleS0zMmJ5dGVzISE=";
    public const string InviteTokenSecret = "dGVzdC1pbnZpdGUtdG9rZW4ta2V5LTAxMjM0NTY3ODk=";
    public const string UnusedUrl = "http://localhost:1";
    public const string UnusedSecret = "unused-in-tests";
    public const string KeycloakRealm = "healthyfac";
    public const string SpaBaseUrl = "https://app.test";
    public const string PublicBlobBaseUrl = "https://api.healthyfac.test/api/document/blobs";

    public const string AuthClientSecret = "test-auth-client-secret";
    public const string KeycloakSpaUrl = "http://localhost:4200";
    public const string TokenPathFormat = "{0}/realms/{1}/protocol/openid-connect/token";
    public const string SupervisorEmail = "supervisor@healthyfac.local";
    public const string SupervisorPassword = "Sup3rvisor-Passw0rd!";

    public static readonly IReadOnlyDictionary<string, string> RealmEnvironment = new Dictionary<string, string>
    {
        ["HEALTHYFAC_SPA_URL"] = KeycloakSpaUrl,
        ["HEALTHYFAC_TEST_COMPANY_ID"] = "0190f3a0-0000-7000-8000-000000000001",
        ["HEALTHYFAC_TEST_SUPERVISOR_IDENTITY_ID"] = "0190f3a0-0000-7000-8000-000000000002",
        ["HEALTHYFAC_TEST_SUPERVISOR_EMPLOYEE_ID"] = "0190f3a0-0000-7000-8000-000000000003",
        ["HEALTHYFAC_TEST_SUPERVISOR_EMAIL"] = SupervisorEmail,
        ["HEALTHYFAC_TEST_SUPERVISOR_PASSWORD"] = SupervisorPassword,
        ["KEYCLOAK_AUTH_CLIENT_SECRET"] = AuthClientSecret,
        ["KEYCLOAK_CORE_CLIENT_SECRET"] = "test-core-client-secret",
        ["KEYCLOAK_TELEMETRY_CLIENT_SECRET"] = "test-telemetry-client-secret",
        ["KEYCLOAK_DOCUMENT_CLIENT_SECRET"] = "test-document-client-secret",
        ["KEYCLOAK_INSIGHTS_CLIENT_SECRET"] = "test-insights-client-secret",
    };

    public static readonly DateTimeOffset FirstMoment = new(
        year: 2027, month: 3, day: 24, hour: 7, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly ActorContext InitializationActor = new()
    {
        Kind = ActorKind.User,
        UserId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000a0ff"),
    };

    public static readonly IReadOnlyList<string> ServiceNames = ["core", "telemetry", "document", "insights", "auth"];
}
