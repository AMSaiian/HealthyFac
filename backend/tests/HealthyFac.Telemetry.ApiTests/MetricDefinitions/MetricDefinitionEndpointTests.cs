using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.ApiTests.MetricDefinitions;

[Collection(TelemetryApiCollection.Name)]
public sealed class MetricDefinitionEndpointTests(TelemetryApiFixture fixture)
{
    private readonly TelemetryApiFixture _fixture = fixture;

    [Fact]
    public async Task List_WithoutReadPermission_Returns403()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(MetricDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(HttpMethod.Get, _fixture.DefinitionsPath(machineId), claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Apply_WithoutWritePermission_Returns403()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(MetricDefinitionApiTestData.ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricDefinitionApiTestData.CreateBody);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSchema_WithoutReadPermission_Returns403()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(MetricDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(HttpMethod.Get, _fixture.SchemaPath(machineId), claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListMetricDefinitions_AfterApply_ReturnsActiveDefinitionsAndRevision()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(
            MetricDefinitionApiTestData.ReadPermission,
            MetricDefinitionApiTestData.WritePermission);
        var applied = await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricDefinitionApiTestData.CreateBody);

        var response = await _fixture.SendAsync(HttpMethod.Get, _fixture.DefinitionsPath(machineId), claims);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
        var definition = body[propertyName: "definitions"]!.AsArray().Single()!;

        applied.StatusCode.Should().Be(HttpStatusCode.OK);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body[propertyName: "revision"]!.GetValue<int>().Should().Be(expected: 1);
        definition[propertyName: "key"]!.GetValue<string>().Should().Be(MetricDefinitionApiTestData.Key);
        definition[propertyName: "valueKind"]!.GetValue<string>().Should().Be(expected: "numeric");
        definition[propertyName: "unit"]![propertyName: "quantityKind"]!.GetValue<string>()
            .Should()
            .Be(expected: "Temperature");
        definition[propertyName: "unit"]![propertyName: "abbreviation"]!.GetValue<string>()
            .Should()
            .Be(expected: "°C");
        _fixture.IdEncryptor.Decrypt(definition[propertyName: "id"]!.GetValue<string>()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMetricSchema_AfterApply_ReturnsLastRevision()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(
            MetricDefinitionApiTestData.ReadPermission,
            MetricDefinitionApiTestData.WritePermission);
        await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricDefinitionApiTestData.CreateBody);

        var response = await _fixture.SendAsync(HttpMethod.Get, _fixture.SchemaPath(machineId), claims);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body[propertyName: "revision"]!.GetValue<int>().Should().Be(expected: 1);
        body[propertyName: "schema"]![propertyName: "properties"]![propertyName: "metrics"]![propertyName: "properties"]
            !.AsObject()
            .ContainsKey(MetricDefinitionApiTestData.Key)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Apply_WithStaleRevision_Returns409()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(MetricDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricDefinitionApiTestData.StaleBody);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Apply_WithInvalidKey_Returns400WithItemPath()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(MetricDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricDefinitionApiTestData.InvalidKeyBody);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain(expected: "created[0].key");
    }

    [Fact]
    public async Task Apply_WithInvalidDeletedId_Returns404()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(MetricDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricDefinitionApiTestData.InvalidDeletedIdBody);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_WithInvalidMachineId_Returns404WithoutLeak()
    {
        var claims = _fixture.CreateClaims(MetricDefinitionApiTestData.ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Get,
            _fixture.DefinitionsPath(MetricDefinitionApiTestData.InvalidEncryptedId),
            claims);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Should().NotContain(MetricDefinitionApiTestData.InvalidEncryptedId);
    }

    [Fact]
    public async Task List_ForMachineOfOtherCompany_Returns404()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(MetricDefinitionApiTestData.ReadPermission) with
        {
            Organizations = [Guid.NewGuid().ToString()],
        };

        var response = await _fixture.SendAsync(HttpMethod.Get, _fixture.DefinitionsPath(machineId), claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
