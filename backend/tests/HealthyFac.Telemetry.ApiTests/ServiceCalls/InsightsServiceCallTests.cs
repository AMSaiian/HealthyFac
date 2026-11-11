using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.ApiTests.MetricUnits.Data;
using HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Telemetry.ApiTests.ServiceCalls;

[Collection(TelemetryApiCollection.Name)]
public sealed class InsightsServiceCallTests(TelemetryApiFixture fixture)
{
    private const string SearchBody = "{\"offset\":0,\"limit\":100,\"filter\":{}}";

    private readonly TelemetryApiFixture _fixture = fixture;

    [Fact]
    public async Task ListMetricUnits_WithInsightsServiceToken_Returns200()
    {
        var response = await SendAsServiceAsync(
            HttpMethod.Get, MetricUnitApiTestData.UnitsPath, MetricUnitApiTestData.ReadPermission, _fixture.CompanyId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListMetricDefinitions_WithInsightsServiceToken_Returns200ForTheActorCompany()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var path = $"{MetricDefinitionApiTestData.MachinesPath}/{machineId}/"
            + MetricDefinitionApiTestData.DefinitionsSegment;

        var response = await SendAsServiceAsync(
            HttpMethod.Get, path, MetricDefinitionApiTestData.ReadPermission, _fixture.CompanyId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchStatusDefinitions_WithInsightsServiceToken_Returns200ForTheActorCompany()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var path = $"{MetricDefinitionApiTestData.MachinesPath}/{machineId}/"
            + $"{StatusDefinitionApiTestData.StatusDefinitionsSegment}/search";

        var response = await SendAsServiceAsync(
            HttpMethod.Post, path, StatusDefinitionApiTestData.ReadPermission, _fixture.CompanyId, SearchBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListMetricDefinitions_WithActorOfAnotherCompany_Returns404()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var path = $"{MetricDefinitionApiTestData.MachinesPath}/{machineId}/"
            + MetricDefinitionApiTestData.DefinitionsSegment;

        var response = await SendAsServiceAsync(
            HttpMethod.Get, path, MetricDefinitionApiTestData.ReadPermission, Guid.CreateVersion7());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchStatusDefinitions_WithActorOfAnotherCompany_Returns404()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var path = $"{MetricDefinitionApiTestData.MachinesPath}/{machineId}/"
            + $"{StatusDefinitionApiTestData.StatusDefinitionsSegment}/search";

        var response = await SendAsServiceAsync(
            HttpMethod.Post, path, StatusDefinitionApiTestData.ReadPermission, Guid.CreateVersion7(), SearchBody);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListMetricUnits_WithoutReadPermission_Returns403()
    {
        var response = await SendAsServiceAsync(
            HttpMethod.Get, MetricUnitApiTestData.UnitsPath, permission: null, _fixture.CompanyId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<HttpResponseMessage> SendAsServiceAsync(
        HttpMethod method,
        string path,
        string? permission,
        Guid actorCompanyId,
        string? jsonBody = null
    )
    {
        using var client = _fixture.CreateClient();
        using var request = new HttpRequestMessage(method, path);
        var claims = new TestServiceTokenClaims
        {
            ClientId = TelemetryApiTestData.InsightsClientId,
            Permissions = permission is null ? [] : [permission],
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            scheme: "Bearer", _fixture.Tokens.CreateServiceToken(claims));
        request.Headers.Add(ActorHeaderNames.ActorId, Guid.CreateVersion7().ToString());
        request.Headers.Add(ActorHeaderNames.CompanyId, actorCompanyId.ToString());

        if (jsonBody is not null)
        {
            request.Content = new StringContent(jsonBody, Encoding.UTF8, mediaType: "application/json");
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
