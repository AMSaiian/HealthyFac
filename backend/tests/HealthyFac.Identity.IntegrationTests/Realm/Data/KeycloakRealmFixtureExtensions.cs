using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

public static class KeycloakRealmFixtureExtensions
{
    private const string RealmAdminPath = "admin/realms/healthyfac";
    private const string TokenPath = "realms/healthyfac/protocol/openid-connect/token";

    extension(KeycloakRealmFixture fixture)
    {
        public async Task<IReadOnlyList<RealmOrganization>> ListOrganizationsAsync()
        {
            var organizations = await fixture.GetAdminJsonAsync<List<RealmOrganization>>(
                $"{RealmAdminPath}/organizations");

            return organizations;
        }

        public async Task<IReadOnlyList<Guid>> ListOrganizationMemberIdsAsync(string organizationId)
        {
            var members = await fixture.GetAdminJsonAsync<List<OrganizationMember>>(
                $"{RealmAdminPath}/organizations/{organizationId}/members");

            return members.Select(member => member.Id).ToList();
        }

        public async Task<IReadOnlyList<string>> ListEditRolesOfUserAttributeAsync(string attributeName)
        {
            var profile = await fixture.GetAdminJsonAsync<JsonObject>($"{RealmAdminPath}/users/profile");
            var attribute = profile["attributes"]!.AsArray()
                .Single(candidate => candidate!["name"]!.GetValue<string>() == attributeName)!;

            return attribute["permissions"]!["edit"]!.AsArray()
                .Select(role => role!.GetValue<string>())
                .ToList();
        }

        public async Task<JsonObject> GetSupervisorAccessTokenClaimsAsync()
        {
            var clientId = await fixture.Server.CreatePasswordGrantClientAsync();

            return await RequestSupervisorTokenClaimsAsync(fixture.Server, clientId);
        }

        public async Task<JsonObject> GetAuthServiceAccessTokenClaimsAsync()
        {
            using var client = new HttpClient { BaseAddress = new Uri(fixture.Server.BaseAddress) };
            using var response = await client.PostAsync(
                TokenPath,
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = RealmTestData.AuthClientId,
                        ["client_secret"] = RealmTestData.AuthClientSecret,
                    }));
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<JsonObject>();

            return DecodePayload(body!["access_token"]!.GetValue<string>());
        }

        private async Task<T> GetAdminJsonAsync<T>(string path)
        {
            var adminToken = await fixture.Server.GetAdminTokenAsync();
            using var client = new HttpClient { BaseAddress = new Uri(fixture.Server.BaseAddress) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var result = await client.GetFromJsonAsync<T>(path);

            return result!;
        }
    }

    internal static async Task<JsonObject> RequestSupervisorTokenClaimsAsync(KeycloakTestServer server, string clientId)
    {
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseAddress) };
        using var response = await client.PostAsync(
            TokenPath,
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = clientId,
                    ["client_secret"] = KeycloakTestServer.PasswordGrantClientSecret,
                    ["username"] = RealmTestData.SupervisorEmail,
                    ["password"] = RealmTestData.SupervisorPassword,
                }));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonObject>();

        return DecodePayload(body!["access_token"]!.GetValue<string>());
    }

    private static JsonObject DecodePayload(string token)
    {
        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');

        return JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(padded)))!.AsObject();
    }
}

public sealed record RealmOrganization
{
    public required string Id { get; init; }

    public required string Alias { get; init; }
}

public sealed record OrganizationMember
{
    public required Guid Id { get; init; }
}
