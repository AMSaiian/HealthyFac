using System.Net;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.ApiTests.StatusDefinitions;

public sealed class StatusDefinitionRouteTests(TelemetryApiFixture fixture) : IClassFixture<TelemetryApiFixture>
{
    private readonly TelemetryApiFixture _fixture = fixture;

    [Fact]
    public async Task Get_WithMalformedMachineId_Returns404()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Get,
            $"{_fixture.StatusDefinitionsPath(StatusDefinitionApiTestData.InvalidEncryptedId)}/{status.Id}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_WithMalformedStatusDefinitionId_Returns404()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Get,
            $"{_fixture.StatusDefinitionsPath(status.MachineId)}/{StatusDefinitionApiTestData.InvalidEncryptedId}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_WithMalformedMachineId_Returns404()
    {
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            $"{_fixture.StatusDefinitionsPath(StatusDefinitionApiTestData.InvalidEncryptedId)}"
            + $"/{StatusDefinitionApiTestData.SearchSegment}",
            claims,
            StatusDefinitionApiTestData.SearchEmptyBody);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
