using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Identity.IntegrationTests.Realm.Data;

namespace HealthyFac.Identity.IntegrationTests.ServiceTokens.Data;

public static class ScopedServiceTokenFixtureExtensions
{
    private const string ScopeField = "scope";

    extension(ScopedServiceTokenFixture fixture)
    {
        /// <summary>
        /// Requests a Client Credentials token for the temporary service client.
        /// </summary>
        /// <param name="scope">The requested scopes separated by spaces, or <see langword="null"/> to send
        /// none.</param>
        /// <returns>The claims of the access token.</returns>
        public async Task<JsonObject> RequestServiceTokenClaimsAsync(string? scope)
        {
            var form = new Dictionary<string, string>
            {
                [key: "grant_type"] = "client_credentials",
                [key: "client_id"] = ScopedServiceTokenTestData.ServiceClientId,
                [key: "client_secret"] = ScopedServiceTokenTestData.ServiceClientSecret,
            };

            if (scope is not null)
            {
                form[ScopeField] = scope;
            }

            var response = await fixture.Realm.RequestTokenAsync(form);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            return response.AccessTokenClaims();
        }
    }

    extension(JsonObject claims)
    {
        public IReadOnlyList<string> Permissions() =>
            claims[RealmTestData.PermissionsClaim]?.AsArray().Select(value => value!.GetValue<string>()).ToList() ?? [];
    }
}
