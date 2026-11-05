using System.Net.Http.Headers;
using System.Net.Http.Json;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Auth.IntegrationTests.Keycloak.Data;

/// <summary>
/// Calls the Keycloak admin API with the admin account of the container. The tests use it to arrange and to read the
/// state, independently of the admin client under test.
/// </summary>
/// <param name="server">The Keycloak container.</param>
public sealed class KeycloakAdminApi(KeycloakTestServer server)
{
    private readonly KeycloakTestServer _server = server;

    public async Task<T> GetAsync<T>(string path)
    {
        using var client = await CreateClientAsync();
        var body = await client.GetFromJsonAsync<T>(path);

        return body!;
    }

    public async Task SendAsync(HttpMethod method, string path, object body)
    {
        using var client = await CreateClientAsync();
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpClient> CreateClientAsync()
    {
        var adminToken = await _server.GetAdminTokenAsync();
        var client = new HttpClient { BaseAddress = new Uri(_server.BaseAddress) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", adminToken);

        return client;
    }
}
