using System.Net;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.ApiTests.StatusDefinitions;

[Collection(TelemetryApiCollection.Name)]
public sealed class StatusDefinitionInputTests(TelemetryApiFixture fixture)
{
    private readonly TelemetryApiFixture _fixture = fixture;

    [Fact]
    public async Task CreateStatusDefinition_WithImageSrcInDescription_Returns400()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            _fixture.StatusDefinitionsPath(machineId),
            claims,
            _fixture.CreateStatusDefinitionBody(
                StatusDefinitionApiTestData.Code,
                StatusDefinitionApiTestData.SrcImageDescription));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateStatusDefinition_WithFileImageInDescription_Returns201()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(StatusDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            _fixture.StatusDefinitionsPath(machineId),
            claims,
            _fixture.CreateStatusDefinitionBody(
                StatusDefinitionApiTestData.Code,
                StatusDefinitionApiTestData.FileImageDescription));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
