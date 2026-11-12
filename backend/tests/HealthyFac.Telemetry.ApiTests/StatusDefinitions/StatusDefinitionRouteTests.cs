using System.Net;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.ApiTests.StatusDefinitions;

[Collection(TelemetryApiCollection.Name)]
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
            .Replace("{machineId}", status.MachineId.ToString())
            .Replace("{statusDefinitionId}", status.Id.ToString());

        var response = await _fixture.SendAsync(new HttpMethod(method), path, claims, body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, because: caseName);
    }

    [Fact]
    public async Task Create_WithMalformedMachineId_Returns404()
    {
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            _fixture.StatusDefinitionsPath(StatusDefinitionApiTestData.InvalidEncryptedId),
            claims,
            _fixture.CreateStatusDefinitionBody(StatusDefinitionApiTestData.Code));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithMalformedStatusDefinitionId_Returns404()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            $"{_fixture.StatusDefinitionsPath(status.MachineId)}/{StatusDefinitionApiTestData.InvalidEncryptedId}",
            claims,
            _fixture.CreateStatusDefinitionBody(StatusDefinitionApiTestData.UpdatedCode));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithMalformedStatusDefinitionId_Returns404()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.DeletePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Delete,
            $"{_fixture.StatusDefinitionsPath(status.MachineId)}/{StatusDefinitionApiTestData.InvalidEncryptedId}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithMalformedMachineId_Returns404()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            $"{_fixture.StatusDefinitionsPath(StatusDefinitionApiTestData.InvalidEncryptedId)}/{status.Id}",
            claims,
            _fixture.CreateStatusDefinitionBody(StatusDefinitionApiTestData.UpdatedCode));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithMalformedMachineId_Returns404()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.DeletePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Delete,
            $"{_fixture.StatusDefinitionsPath(StatusDefinitionApiTestData.InvalidEncryptedId)}/{status.Id}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
