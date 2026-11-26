using System.Net;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.ApiTests.StatusDefinitions;

[Collection(TelemetryApiCollection.Name)]
[Feature(TestFeatures.SignalModel)]
public sealed class StatusDefinitionRouteTests(TelemetryApiFixture fixture)
{
    private readonly TelemetryApiFixture _fixture = fixture;

    [Theory]
    [MemberData(
        nameof(StatusDefinitionApiTestData.MalformedIdReadRoutes),
        MemberType = typeof(StatusDefinitionApiTestData)
    )]
    public async Task MalformedId_WithReadPermission_Returns404(
        string caseName,
        string method,
        string pathTemplate,
        string? body
    )
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.ReadPermission);

        var path = pathTemplate
            .Replace(oldValue: "{machineId}", status.MachineId.ToString())
            .Replace(oldValue: "{statusDefinitionId}", status.Id.ToString());

        var response = await _fixture.SendAsync(new HttpMethod(method), path, claims, body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, caseName);
    }

    [Theory]
    [MemberData(
        nameof(StatusDefinitionApiTestData.MalformedIdWriteRoutes),
        MemberType = typeof(StatusDefinitionApiTestData)
    )]
    public async Task MalformedId_WithWriteOrDeletePermission_Returns404(
        string caseName,
        string method,
        string permission,
        string pathTemplate,
        Func<TelemetryApiFixture, string?> createBody
    )
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(permission);

        var path = pathTemplate
            .Replace(oldValue: "{machineId}", status.MachineId.ToString())
            .Replace(oldValue: "{statusDefinitionId}", status.Id.ToString());

        var response = await _fixture.SendAsync(new HttpMethod(method), path, claims, createBody(_fixture));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, caseName);
    }
}
