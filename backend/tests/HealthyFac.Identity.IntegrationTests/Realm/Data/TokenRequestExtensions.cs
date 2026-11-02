using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

/// <summary>
/// Sends requests to the token endpoint of the realm, as a service does.
/// </summary>
public static class TokenRequestExtensions
{
    private const string TokenPath = "realms/healthyfac/protocol/openid-connect/token";
    private const string AudienceClaim = "aud";

    extension(KeycloakRealmFixture fixture)
    {
        /// <summary>
        /// Posts a form to the token endpoint.
        /// </summary>
        /// <param name="form">The form fields of the grant.</param>
        /// <param name="host">The <c>Host</c> header to send, or <see langword="null"/> for the address of the
        /// container. A service sends the internal name of Keycloak.</param>
        /// <returns>The status code and the JSON body of the answer.</returns>
        public async Task<TokenResponse> RequestTokenAsync(
            IReadOnlyDictionary<string, string> form,
            string? host = null
        )
        {
            using var client = new HttpClient { BaseAddress = new Uri(fixture.Server.BaseAddress) };
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenPath)
            {
                Content = new FormUrlEncodedContent(form),
            };

            if (host is not null)
            {
                request.Headers.Host = host;
            }

            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadFromJsonAsync<JsonObject>();

            return new TokenResponse { StatusCode = response.StatusCode, Body = body! };
        }

        public async Task<string> GetSupervisorAccessTokenAsync()
        {
            var clientId = await fixture.Server.CreatePasswordGrantClientAsync();
            var response = await fixture.RequestTokenAsync(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = clientId,
                    ["client_secret"] = KeycloakTestServer.PasswordGrantClientSecret,
                    ["username"] = RealmTestData.SupervisorEmail,
                    ["password"] = RealmTestData.SupervisorPassword,
                });
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            return response.Body["access_token"]!.GetValue<string>();
        }
    }

    extension(TokenResponse response)
    {
        public JsonObject AccessTokenClaims() =>
            KeycloakRealmFixtureExtensions.DecodePayload(response.Body["access_token"]!.GetValue<string>());
    }

    extension(JsonObject claims)
    {
        public IReadOnlyList<string> Audiences() =>
            claims[AudienceClaim] switch
            {
                JsonArray audiences => audiences.Select(audience => audience!.GetValue<string>()).ToList(),
                JsonValue audience => [audience.GetValue<string>()],
                _ => [],
            };
    }
}

public sealed record TokenResponse
{
    public required HttpStatusCode StatusCode { get; init; }

    public required JsonObject Body { get; init; }
}
