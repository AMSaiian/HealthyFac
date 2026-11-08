using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Companies.Data;
using HealthyFac.Core.ApiTests.Employees.Data;
using HealthyFac.Core.ApiTests.Hosting;

namespace HealthyFac.Core.ApiTests.Companies;

[Collection(CoreApiCollection.Name)]
public sealed class CompanyApiTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task GetCompany_WithUserToken_ReturnsNameAndTimeZone()
    {
        var response = await _fixture.GetCompanyAsync(_fixture.CreateSupervisorClaims());
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body![propertyName: "name"]!.GetValue<string>().Should().Be(CoreApiTestData.CompanyName);
        body[propertyName: "timeZoneId"]!.GetValue<string>().Should().Be(CoreApiTestData.CompanyTimeZoneId);
    }

    [Fact]
    public async Task GetCompany_WithoutToken_Returns401()
    {
        var response = await _fixture.GetCompanyAsync(claims: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
