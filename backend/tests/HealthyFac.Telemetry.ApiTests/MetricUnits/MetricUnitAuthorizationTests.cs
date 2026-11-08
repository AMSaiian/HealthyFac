using System.Net;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.MetricUnits.Data;

namespace HealthyFac.Telemetry.ApiTests.MetricUnits;

[Collection(TelemetryApiCollection.Name)]
public sealed class MetricUnitAuthorizationTests(TelemetryApiFixture fixture)
{
    private readonly TelemetryApiFixture _fixture = fixture;

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync(MetricUnitApiTestData.UnitsPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_WithoutReadPermission_Returns403()
    {
        var claims = _fixture.CreateClaims(MetricUnitApiTestData.WritePermission);

        var response = await _fixture.SendAsync(HttpMethod.Get, MetricUnitApiTestData.UnitsPath, claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_WithReadPermission_Returns200WithCatalog()
    {
        var claims = _fixture.CreateClaims(MetricUnitApiTestData.ReadPermission);

        var response = await _fixture.SendAsync(HttpMethod.Get, MetricUnitApiTestData.UnitsPath, claims);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(expected: "DegreeCelsius");
    }
}
