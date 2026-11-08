using System.Net.Http.Headers;
using System.Text;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Telemetry.ApiTests.Hosting;

public static class TelemetryApiClientExtensions
{
    private const string BearerScheme = "Bearer";
    private const string JsonMediaType = "application/json";

    extension(TelemetryApiFixture fixture)
    {
        public TestTokenClaims CreateClaims(params string[] permissions) =>
            new()
            {
                Subject = Guid.NewGuid(),
                Organizations = [fixture.CompanyId.ToString()],
                Permissions = permissions,
            };

        public async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string path,
            TestTokenClaims claims,
            string? jsonBody = null
        )
        {
            using var client = fixture.CreateClient();
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                BearerScheme,
                fixture.Tokens.CreateToken(claims));

            if (jsonBody is not null)
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, JsonMediaType);
            }

            return await client.SendAsync(request);
        }
    }
}
