using Bogus;
using HealthyFac.Auth.Application.Identities;

namespace HealthyFac.Auth.IntegrationTests.Keycloak.Data;

/// <summary>
/// The fixed values for every placeholder of the realm file, the settings of the Keycloak admin client, and the
/// identities that the tests create.
/// </summary>
public static class KeycloakAdminTestData
{
    public const string RealmName = "healthyfac";
    public const string AuthClientId = "healthyfac-auth";
    public const string AuthClientSecret = "test-auth-client-secret";
    public const string SpaClientId = "healthyfac-spa";
    public const string SpaUrl = "http://localhost:4200";
    public const string SupervisorEmail = "supervisor@healthyfac.local";
    public const string SupervisorPassword = "Sup3rvisor-Passw0rd!";
    public const string WorkerRoleName = "worker";
    public const string UnknownRoleName = "no-such-role";
    public const string EmailDomain = "healthyfac.test";
    public const string NewPassword = "N3w-Worker-Passw0rd!";
    public const string TokenPathFormat = "{0}/realms/{1}/protocol/openid-connect/token";
    public const int FakerSeed = 20261028;

    public static readonly Guid CompanyId = Guid.Parse("0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid UnknownCompanyId = Guid.Parse("0190f3a0-0000-7000-8000-0000000000ff");
    public static readonly Guid SupervisorIdentityId = Guid.Parse("0190f3a0-0000-7000-8000-000000000002");
    public static readonly Guid SupervisorEmployeeId = Guid.Parse("0190f3a0-0000-7000-8000-000000000003");

    public static IReadOnlyDictionary<string, string> RealmEnvironment { get; } = new Dictionary<string, string>
    {
        ["HEALTHYFAC_SPA_URL"] = SpaUrl,
        ["HEALTHYFAC_TEST_COMPANY_ID"] = CompanyId.ToString(),
        ["HEALTHYFAC_TEST_SUPERVISOR_IDENTITY_ID"] = SupervisorIdentityId.ToString(),
        ["HEALTHYFAC_TEST_SUPERVISOR_EMPLOYEE_ID"] = SupervisorEmployeeId.ToString(),
        ["HEALTHYFAC_TEST_SUPERVISOR_EMAIL"] = SupervisorEmail,
        ["HEALTHYFAC_TEST_SUPERVISOR_PASSWORD"] = SupervisorPassword,
        ["KEYCLOAK_AUTH_CLIENT_SECRET"] = AuthClientSecret,
    };

    /// <summary>
    /// Creates an identity with a new employee ID and an email that no other identity of the run shares. The names
    /// come from a faker with a fixed seed.
    /// </summary>
    /// <returns>The new identity.</returns>
    public static NewIdentity CreateIdentity()
    {
        var faker = new Faker { Random = new Randomizer(FakerSeed) };
        var employeeId = Guid.CreateVersion7();

        return new NewIdentity
        {
            EmployeeId = employeeId,
            Email = $"{employeeId:N}@{EmailDomain}",
            FirstName = faker.Name.FirstName(),
            LastName = faker.Name.LastName(),
        };
    }
}
