using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

/// <summary>
/// Creates the temporary clients, roles, and scopes of the token tests through the admin API, so the realm file
/// keeps no test-only entity.
/// </summary>
public static class KeycloakRealmAdminExtensions
{
    private const string RealmAdminPath = "admin/realms/healthyfac";
    private const string BearerScheme = "Bearer";

    extension(KeycloakRealmFixture fixture)
    {
        public async Task<string> CreateClientAsync(JsonObject client)
        {
            using var response = await fixture.SendAdminAsync(HttpMethod.Post, "clients", client);

            return ReadCreatedId(response);
        }

        public async Task<string> FindClientUuidAsync(string clientId)
        {
            var clients = await fixture.GetAdminJsonAsync<JsonArray>($"clients?clientId={clientId}");

            return clients.Single()!["id"]!.GetValue<string>();
        }

        public async Task<JsonObject> CreateClientRoleAsync(string clientUuid, string roleName)
        {
            using var response = await fixture.SendAdminAsync(
                HttpMethod.Post,
                $"clients/{clientUuid}/roles",
                new JsonObject { ["name"] = roleName });
            response.EnsureSuccessStatusCode();

            return await fixture.GetAdminJsonAsync<JsonObject>($"clients/{clientUuid}/roles/{roleName}");
        }

        public async Task<JsonObject> CreateRealmRoleAsync(string roleName, params JsonObject[] composites)
        {
            using var response = await fixture.SendAdminAsync(
                HttpMethod.Post,
                "roles",
                new JsonObject { ["name"] = roleName });
            response.EnsureSuccessStatusCode();

            using var compositesResponse = await fixture.SendAdminAsync(
                HttpMethod.Post,
                $"roles/{roleName}/composites",
                new JsonArray(composites.Select(role => role.DeepClone()).ToArray()));
            compositesResponse.EnsureSuccessStatusCode();

            return await fixture.GetAdminJsonAsync<JsonObject>($"roles/{roleName}");
        }

        public async Task<string> CreateClientScopeAsync(string name, params JsonObject[] mappers)
        {
            using var response = await fixture.SendAdminAsync(
                HttpMethod.Post,
                "client-scopes",
                new JsonObject
                {
                    ["name"] = name,
                    ["protocol"] = "openid-connect",
                    ["attributes"] = new JsonObject { ["include.in.token.scope"] = "true" },
                    ["protocolMappers"] = new JsonArray(
                        mappers.Select(mapper => mapper.DeepClone()).ToArray()),
                });

            return ReadCreatedId(response);
        }

        public async Task AddClientScopeAsync(string clientUuid, string scopeId, bool isDefault)
        {
            var kind = isDefault ? "default" : "optional";
            using var response = await fixture.SendAdminAsync(
                HttpMethod.Put,
                $"clients/{clientUuid}/{kind}-client-scopes/{scopeId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task MapClientRoleToScopeAsync(string scopeId, string roleClientUuid, JsonObject role)
        {
            using var response = await fixture.SendAdminAsync(
                HttpMethod.Post,
                $"client-scopes/{scopeId}/scope-mappings/clients/{roleClientUuid}",
                new JsonArray(role.DeepClone()));
            response.EnsureSuccessStatusCode();
        }

        public async Task<string> GetServiceAccountUserIdAsync(string clientUuid)
        {
            var user = await fixture.GetAdminJsonAsync<JsonObject>($"clients/{clientUuid}/service-account-user");

            return user["id"]!.GetValue<string>();
        }

        public async Task AssignRealmRoleAsync(string userId, JsonObject role)
        {
            using var response = await fixture.SendAdminAsync(
                HttpMethod.Post,
                $"users/{userId}/role-mappings/realm",
                new JsonArray(role.DeepClone()));
            response.EnsureSuccessStatusCode();
        }

        public async Task SetUserEnabledAsync(Guid userId, bool isEnabled)
        {
            var user = await fixture.GetAdminJsonAsync<JsonObject>($"users/{userId}");
            user["enabled"] = isEnabled;

            using var response = await fixture.SendAdminAsync(HttpMethod.Put, $"users/{userId}", user);
            response.EnsureSuccessStatusCode();
        }

        private async Task<T> GetAdminJsonAsync<T>(string path)
        {
            using var response = await fixture.SendAdminAsync(HttpMethod.Get, path);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        private async Task<HttpResponseMessage> SendAdminAsync(
            HttpMethod method,
            string path,
            JsonNode? body = null
        )
        {
            var adminToken = await fixture.Server.GetAdminTokenAsync();
            using var client = new HttpClient { BaseAddress = new Uri(fixture.Server.BaseAddress) };
            using var request = new HttpRequestMessage(method, $"{RealmAdminPath}/{path}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue(BearerScheme, adminToken) },
            };

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            return await client.SendAsync(request);
        }
    }

    private static string ReadCreatedId(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        return response.Headers.Location!.Segments[^1];
    }
}
