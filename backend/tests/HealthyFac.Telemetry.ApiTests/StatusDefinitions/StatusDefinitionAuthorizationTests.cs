using System.Net;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.ApiTests.StatusDefinitions;

public sealed class StatusDefinitionAuthorizationTests(TelemetryApiFixture fixture) : IClassFixture<TelemetryApiFixture>
{
    private readonly TelemetryApiFixture _fixture = fixture;

    [Fact]
    public async Task Create_WithoutWritePermission_Returns403()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            _fixture.StatusDefinitionsPath(machineId),
            claims,
            _fixture.CreateStatusDefinitionBody(StatusDefinitionApiTestData.Code));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithWritePermission_Returns201WithEncryptedIdLocation()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            _fixture.StatusDefinitionsPath(machineId),
            claims,
            _fixture.CreateStatusDefinitionBody(StatusDefinitionApiTestData.Code));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().StartWith("/" + _fixture.StatusDefinitionsPath(machineId) + "/");
        body.Should().Contain(response.Headers.Location.ToString().Split(separator: '/').Last());
    }

    [Fact]
    public async Task Create_WithSameCodeTwice_Returns409()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.WritePermission);
        await _fixture.SendAsync(
            HttpMethod.Post,
            _fixture.StatusDefinitionsPath(machineId),
            claims,
            _fixture.CreateStatusDefinitionBody(StatusDefinitionApiTestData.Code));

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            _fixture.StatusDefinitionsPath(machineId),
            claims,
            _fixture.CreateStatusDefinitionBody(StatusDefinitionApiTestData.Code));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Search_WithoutReadPermission_Returns403()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            $"{_fixture.StatusDefinitionsPath(machineId)}/{StatusDefinitionApiTestData.SearchSegment}",
            claims,
            StatusDefinitionApiTestData.SearchEmptyBody);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Search_WithReadPermission_Returns200()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            $"{_fixture.StatusDefinitionsPath(machineId)}/{StatusDefinitionApiTestData.SearchSegment}",
            claims,
            StatusDefinitionApiTestData.SearchEmptyBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_WithoutReadPermission_Returns403()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Get,
            $"{_fixture.StatusDefinitionsPath(status.MachineId)}/{status.Id}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_WithReadPermission_ReturnsStatusDefinition()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Get,
            $"{_fixture.StatusDefinitionsPath(status.MachineId)}/{status.Id}",
            claims);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(status.Id);
    }

    [Fact]
    public async Task Update_WithoutWritePermission_Returns403()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            $"{_fixture.StatusDefinitionsPath(status.MachineId)}/{status.Id}",
            claims,
            _fixture.CreateStatusDefinitionBody(StatusDefinitionApiTestData.UpdatedCode));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_WithWritePermission_Returns204()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            $"{_fixture.StatusDefinitionsPath(status.MachineId)}/{status.Id}",
            claims,
            _fixture.CreateStatusDefinitionBody(StatusDefinitionApiTestData.UpdatedCode));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithoutDeletePermission_Returns403()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(
            StatusDefinitionApiTestData.ReadPermission,
            StatusDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Delete,
            $"{_fixture.StatusDefinitionsPath(status.MachineId)}/{status.Id}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_WithDeletePermission_Returns204()
    {
        var status = await _fixture.ArrangeStatusDefinitionAsync(
            StatusDefinitionApiTestData.Code);
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.DeletePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Delete,
            $"{_fixture.StatusDefinitionsPath(status.MachineId)}/{status.Id}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
