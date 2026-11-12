using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Core.ApiTests.Employees.Data;

public static class EmployeeApiFixtureExtensions
{
    private const string BearerScheme = "Bearer";

    extension(CoreApiFixture fixture)
    {
        public async Task<HttpResponseMessage> PostEmployeeActionAsync(
            string path,
            TestTokenClaims claims,
            Guid employeeId
        )
        {
            using var client = fixture.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                BearerScheme,
                fixture.Tokens.CreateToken(claims));

            return await client.PostAsJsonAsync(
                path,
                new JsonObject { [propertyName: "employeeId"] = fixture.IdEncryptor.Encrypt(employeeId) });
        }

        public async Task<HttpResponseMessage> PostEmployeeActionWithoutBodyAsync(string path, TestTokenClaims claims)
        {
            using var client = fixture.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                BearerScheme,
                fixture.Tokens.CreateToken(claims));

            return await client.PostAsync(path, content: null);
        }

        public async Task<HttpResponseMessage> PostEmployeeSearchAsync(string path, TestTokenClaims claims)
        {
            using var client = fixture.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                BearerScheme,
                fixture.Tokens.CreateToken(claims));

            return await client.PostAsJsonAsync(
                path,
                new JsonObject
                {
                    [propertyName: "offset"] = 0,
                    [propertyName: "limit"] = 10,
                    [propertyName: "filter"] = new JsonObject(),
                });
        }

        public async Task<HttpResponseMessage> GetEmployeeAsync(string path, TestTokenClaims claims)
        {
            using var client = fixture.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                BearerScheme,
                fixture.Tokens.CreateToken(claims));

            return await client.GetAsync(path);
        }
    }
}
