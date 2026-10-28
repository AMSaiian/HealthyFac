using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Testcontainers.Keycloak;

namespace HealthyFac.Testing.Containers;

/// <summary>
/// Runs a Keycloak container for integration tests, with the image from <see cref="TestImages"/>. The container
/// imports the development realm file of the repository, so tests run against the realm that development uses.
/// </summary>
public sealed class KeycloakTestServer : IAsyncDisposable
{
    /// <summary>The client secret of every client that <see cref="CreatePasswordGrantClientAsync"/> creates.</summary>
    public const string PasswordGrantClientSecret = "test-password-grant-secret";

    private const string AdminUsername = "admin";
    private const string AdminPassword = "test-admin-password";
    private const string AdminClientId = "admin-cli";
    private const string MasterRealmName = "master";
    private const string RealmName = "healthyfac";
    private const string RealmImportDirectory = "import";
    private const string RealmFileName = "healthyfac-realm.json";
    private const string TokenPathFormat = "realms/{0}/protocol/openid-connect/token";
    private const string ClientsPath = $"admin/realms/{RealmName}/clients";
    private const string PasswordGrantClientIdPrefix = "test-password-grant-";

    private static readonly string[] PasswordGrantClientScopes =
    [
        "basic",
        "organization",
        "healthyfac-claims",
    ];

    private readonly HttpClient _client = new();

    private KeycloakContainer? _container;

    public string BaseAddress => _container!.GetBaseAddress();

    /// <summary>
    /// Starts the container and imports the realm file. Keycloak replaces each <c>${NAME}</c> placeholder of the
    /// realm file with the environment variable of the same name.
    /// </summary>
    /// <param name="environment">The environment variables of the container, one for each realm placeholder.</param>
    public async Task StartAsync(IReadOnlyDictionary<string, string> environment)
    {
        var builder = new KeycloakBuilder(TestImages.Keycloak)
            .WithUsername(AdminUsername)
            .WithPassword(AdminPassword)
            .WithRealm(KeycloakFiles.Find(RealmImportDirectory, RealmFileName));

        foreach (var (name, value) in environment)
        {
            builder = builder.WithEnvironment(name, value);
        }

        _container = builder.Build();
        await _container.StartAsync();

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
                        PasswordGrantClientScopes.Select(scope => (JsonNode)JsonValue.Create(scope)!).ToArray()),
                }),
        };

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return clientId;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
