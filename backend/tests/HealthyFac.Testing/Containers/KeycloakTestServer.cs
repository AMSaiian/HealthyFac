using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.Keycloak;

namespace HealthyFac.Testing.Containers;

/// <summary>
/// Runs a Keycloak container for integration tests, with the images from <see cref="TestImages"/>. The
/// <c>keycloak-config-cli</c> job applies the development realm file of the repository to the running Keycloak, as
/// the <c>keycloak-config</c> service of <c>docker-compose.yml</c> does, so tests run against the realm that
/// development uses.
/// </summary>
public sealed class KeycloakTestServer : IAsyncDisposable
{
    /// <summary>The client secret of every client that <see cref="CreatePasswordGrantClientAsync"/> creates.</summary>
    public const string PasswordGrantClientSecret = "test-password-grant-secret";

    /// <summary>
    /// The name under which the container reaches the Docker host (e.g., a Mailpit container on a published port).
    /// </summary>
    public const string HostGatewayName = "host.docker.internal";

    private const string HostGatewayAddress = "host-gateway";
    private const string AdminUsername = "admin";
    private const string AdminPassword = "test-admin-password";
    private const string AdminClientId = "admin-cli";
    private const string MasterRealmName = "master";
    private const string RealmName = "healthyfac";
    private const string BootstrapDirectory = "import";
    private const string BootstrapFileName = "healthyfac-test-bootstrap.json";
    private const string RealmDirectory = "realm";
    private const string RealmFileName = "healthyfac-realm.json";
    private const string TestTenantDirectory = "dev";
    private const string TestTenantFileName = "healthyfac-test-tenant.json";
    private const string KeycloakVariablePrefix = "KC_";
    private const string SupervisorIdentityIdVariable = "HEALTHYFAC_TEST_SUPERVISOR_IDENTITY_ID";
    private const string SupervisorEmailVariable = "HEALTHYFAC_TEST_SUPERVISOR_EMAIL";
    private const string KeycloakNetworkAlias = "keycloak";
    private const string KeycloakInternalAddress = $"http://{KeycloakNetworkAlias}:8080";
    private const string ConfigDirectory = "/config";
    private const string TokenPathFormat = "realms/{0}/protocol/openid-connect/token";
    private const string ClientsPath = $"admin/realms/{RealmName}/clients";
    private const string PasswordGrantClientIdPrefix = "test-password-grant-";

    private static readonly string[] PasswordGrantClientScopes =
    [
        "basic",
        "organization",
        "healthyfac-claims",
    ];

    private static readonly TimeSpan ConfigJobTimeout = TimeSpan.FromSeconds(120);

    private static readonly IReadOnlyDictionary<string, string> ConfigJobOptions = new Dictionary<string, string>
    {
        ["IMPORT_VARSUBSTITUTION_ENABLED"] = "true",
        ["IMPORT_VARSUBSTITUTION_PREFIX"] = "${",
        ["IMPORT_VARSUBSTITUTION_SUFFIX"] = "}",
        ["IMPORT_VARSUBSTITUTION_UNDEFINEDISERROR"] = "false",
        ["IMPORT_MANAGED_ORGANIZATION"] = "no-delete",
        ["IMPORT_USERS_MERGEROLES"] = "true",
    };

    private readonly HttpClient _client = new();

    private INetwork? _network;
    private KeycloakContainer? _container;
    private IContainer? _configJob;

    public string BaseAddress => _container!.GetBaseAddress();

    /// <summary>
    /// Starts Keycloak with the bootstrap import, which creates the realm and the supervisor with the fixed ID. Then
    /// runs the <c>keycloak-config-cli</c> job, which applies the realm file and the test tenant file. Keycloak and
    /// the job replace each <c>${NAME}</c> placeholder with the environment variable of the same name.
    /// </summary>
    /// <param name="environment">The environment variables of the job, one for each realm placeholder.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when a value in <paramref name="environment"/> is blank. The job does not stop on a missing value, so
    /// the check reports the variable name before any container starts.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the job exits with a non-zero code. The message holds the exit code and the job log.
    /// </exception>
    public async Task StartAsync(IReadOnlyDictionary<string, string> environment)
    {
        var blankNames = environment.Where(entry => string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.Key);

        if (blankNames.Any())
        {
            throw new ArgumentException(
                $"The environment variables have blank values: {string.Join(", ", blankNames)}.",
                nameof(environment));
        }

        _network = new NetworkBuilder().Build();
        await _network.CreateAsync();

        _container = new KeycloakBuilder(TestImages.Keycloak)
            .WithUsername(AdminUsername)
            .WithPassword(AdminPassword)
            .WithExtraHost(HostGatewayName, HostGatewayAddress)
            .WithNetwork(_network)
            .WithNetworkAliases(KeycloakNetworkAlias)
            .WithRealm(KeycloakFiles.Find(BootstrapDirectory, BootstrapFileName))
            .WithEnvironment(environment.Where(IsKeycloakVariable).ToDictionary())
            .Build();
        await _container.StartAsync();

        var jobBuilder = new ContainerBuilder(TestImages.KeycloakConfigCli)
            .WithNetwork(_network)
            .WithResourceMapping(KeycloakFiles.Find(RealmDirectory, RealmFileName), $"{ConfigDirectory}/")
            .WithResourceMapping(KeycloakFiles.Find(TestTenantDirectory, TestTenantFileName), $"{ConfigDirectory}/")
            .WithEnvironment("KEYCLOAK_URL", KeycloakInternalAddress)
            .WithEnvironment("KEYCLOAK_USER", AdminUsername)
            .WithEnvironment("KEYCLOAK_PASSWORD", AdminPassword)
            .WithEnvironment(
                "IMPORT_FILES_LOCATIONS",
                $"{ConfigDirectory}/{RealmFileName},{ConfigDirectory}/{TestTenantFileName}")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .AddCustomWaitStrategy(new NeverReady(), strategy => strategy.WithTimeout(ConfigJobTimeout)));

        foreach (var (name, value) in ConfigJobOptions.Concat(environment.Where(entry => !IsKeycloakSetting(entry))))
        {
            jobBuilder = jobBuilder.WithEnvironment(name, value);
        }

        _configJob = jobBuilder.Build();

        try
        {
            await _configJob.StartAsync();
        }
        catch (ContainerNotRunningException)
        {
        }

        var exitCode = await _configJob.GetExitCodeAsync();

        if (exitCode != 0)
        {
            var (stdout, stderr) = await _configJob.GetLogsAsync();

            throw new InvalidOperationException(
                $"The keycloak-config-cli job exited with code {exitCode}.{Environment.NewLine}{stdout}{stderr}");
        }

        _client.BaseAddress = new Uri(BaseAddress);
    }

    public async Task<string> GetAdminTokenAsync()
    {
        using var response = await _client.PostAsync(
            string.Format(TokenPathFormat, MasterRealmName),
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = AdminClientId,
                    ["username"] = AdminUsername,
                    ["password"] = AdminPassword,
                }));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonObject>();

        return body!["access_token"]!.GetValue<string>();
    }

    /// <summary>
    /// Creates a temporary confidential client in the realm. Its default client scopes match the single-page app
    /// client, so a password grant returns the same claims that the app receives.
    /// </summary>
    /// <returns>The client ID. The client secret is <see cref="PasswordGrantClientSecret"/>.</returns>
    public async Task<string> CreatePasswordGrantClientAsync()
    {
        var clientId = $"{PasswordGrantClientIdPrefix}{Guid.NewGuid():N}";
        var adminToken = await GetAdminTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, ClientsPath)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", adminToken) },
            Content = JsonContent.Create(
                new JsonObject
                {
                    ["clientId"] = clientId,
                    ["enabled"] = true,
                    ["publicClient"] = false,
                    ["secret"] = PasswordGrantClientSecret,
                    ["standardFlowEnabled"] = false,
                    ["directAccessGrantsEnabled"] = true,
                    ["defaultClientScopes"] = new JsonArray(
                        PasswordGrantClientScopes.Select(scope => (JsonNode)JsonValue.Create(scope)).ToArray()),
                }),
        };

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return clientId;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();

        if (_configJob is not null)
        {
            await _configJob.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        if (_network is not null)
        {
            await _network.DisposeAsync();
        }
    }

    private static bool IsKeycloakSetting(KeyValuePair<string, string> entry)
    {
        return entry.Key.StartsWith(KeycloakVariablePrefix, StringComparison.Ordinal);
    }

    private static bool IsKeycloakVariable(KeyValuePair<string, string> entry)
    {
        return IsKeycloakSetting(entry) || entry.Key is SupervisorIdentityIdVariable or SupervisorEmailVariable;
    }

    /// <summary>
    /// Testcontainers checks that a container still runs on each wait round and throws
    /// <see cref="ContainerNotRunningException"/> after it exits. This wait never succeeds, so the exit of the job is
    /// the only way out before the timeout.
    /// </summary>
    private sealed class NeverReady : IWaitUntil
    {
        public Task<bool> UntilAsync(IContainer container)
        {
            return Task.FromResult(false);
        }
    }
}
