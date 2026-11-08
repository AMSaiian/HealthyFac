using System.Net.Http.Headers;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Core.ApiTests.Companies.Data;

public static class CompanyApiFixtureExtensions
{
    private const string BearerScheme = "Bearer";

    extension(CoreApiFixture fixture)
    {
        public async Task<HttpResponseMessage> GetCompanyAsync(TestTokenClaims? claims)
        {
            using var client = fixture.CreateClient();
            if (claims is not null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    BearerScheme,
                    fixture.Tokens.CreateToken(claims));
            }

            return await client.GetAsync(CompanyApiTestData.CompanyPath);
        }
    }
}
