using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Shared.Web.Authentication;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

public static class SharedPermissionsClaimFixtureExtensions
{
    private const string RealmAdminPath = "admin/realms/healthyfac";
    private const string ClientRoleMapper = "oidc-usermodel-client-role-mapper";
    private const string OpenIdConnect = "openid-connect";

    extension(SharedPermissionsClaimFixture fixture)
    {
        /// <summary>
        /// Gives the supervisor a permission of a second API, adds a second permission mapper for that API to a
        /// temporary client, and reads the <c>permissions</c> claim of the supervisor token of that client.
        /// </summary>
        /// <returns>The values of the <c>permissions</c> claim.</returns>
        public async Task<IReadOnlyList<string>> GetSupervisorPermissionsWithTwoMappersAsync()
        {
            var secondApiUuid = await fixture.CreateConfidentialClientAsync(
                SharedPermissionsClaimTestData.SecondApiClientId);
            var role = await fixture.CreateClientRoleAsync(
                secondApiUuid,
                SharedPermissionsClaimTestData.SecondApiPermission);
            await fixture.AssignClientRoleToSupervisorAsync(secondApiUuid, role);

            var tokenClientId = await fixture.Server.CreatePasswordGrantClientAsync();
            var tokenClientUuid = await fixture.FindClientUuidAsync(tokenClientId);
            await fixture.AddPermissionsMapperAsync(tokenClientUuid, SharedPermissionsClaimTestData.SecondApiClientId);

            var claims = await KeycloakRealmFixtureExtensions.RequestSupervisorTokenClaimsAsync(
                fixture.Server,
                tokenClientId);

            return claims[KeycloakClaimTypes.Permissions]!.AsArray()
                .Select(value => value!.GetValue<string>())
                .ToList();
        }

        private async Task<string> CreateConfidentialClientAsync(string clientId)
        {
            using var response = await fixture.SendAdminAsync(
                HttpMethod.Post,
                $"{RealmAdminPath}/clients",
                new JsonObject { ["clientId"] = clientId, ["enabled"] = true, ["publicClient"] = false });

            return response.Headers.Location!.Segments.Last();
        }

        private async Task<JsonObject> CreateClientRoleAsync(string clientUuid, string roleName)
        {
            using var created = await fixture.SendAdminAsync(
                HttpMethod.Post,
                $"{RealmAdminPath}/clients/{clientUuid}/roles",
                new JsonObject { ["name"] = roleName });
            using var response = await fixture.SendAdminAsync(
                HttpMethod.Get,
                $"{RealmAdminPath}/clients/{clientUuid}/roles/{Uri.EscapeDataString(roleName)}");

            var role = await response.Content.ReadFromJsonAsync<JsonObject>();

            return role!;
        }

        private async Task AssignClientRoleToSupervisorAsync(string clientUuid, JsonObject role)
        {
            using var response = await fixture.SendAdminAsync(
                HttpMethod.Post,
                $"{RealmAdminPath}/users/{RealmTestData.SupervisorIdentityId}/role-mappings/clients/{clientUuid}",
                new JsonArray(role.DeepClone()));
        }

        private async Task<string> FindClientUuidAsync(string clientId)
        {
            using var response = await fixture.SendAdminAsync(
                HttpMethod.Get,
                $"{RealmAdminPath}/clients?clientId={Uri.EscapeDataString(clientId)}");

            var clients = await response.Content.ReadFromJsonAsync<JsonArray>();

            return clients![0]!["id"]!.GetValue<string>();
        }

        private async Task AddPermissionsMapperAsync(string clientUuid, string mappedClientId)
        {
            using var response = await fixture.SendAdminAsync(
                HttpMethod.Post,
                $"{RealmAdminPath}/clients/{clientUuid}/protocol-mappers/models",
                new JsonObject
                {
                    ["name"] = $"{mappedClientId}-permissions",
                    ["protocol"] = OpenIdConnect,
                    ["protocolMapper"] = ClientRoleMapper,
                    ["config"] = new JsonObject
                    {
                        ["usermodel.clientRoleMapping.clientId"] = mappedClientId,
                        ["claim.name"] = KeycloakClaimTypes.Permissions,
                        ["jsonType.label"] = "String",
                        ["multivalued"] = "true",
                        ["access.token.claim"] = "true",
                        ["id.token.claim"] = "false",
                        ["userinfo.token.claim"] = "false",
                    },
                });
        }

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string path, JsonNode? body = null)
        {
            var adminToken = await fixture.Server.GetAdminTokenAsync();
            using var client = new HttpClient { BaseAddress = new Uri(fixture.Server.BaseAddress) };
            using var request = new HttpRequestMessage(method, path)
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", adminToken) },
                Content = body is null ? null : JsonContent.Create(body),
            };

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return response;
        }
    }
}
