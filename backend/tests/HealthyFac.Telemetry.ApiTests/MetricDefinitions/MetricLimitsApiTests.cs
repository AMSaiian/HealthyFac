using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Telemetry.ApiTests.MetricDefinitions;

[Collection(TelemetryApiCollection.Name)]
public sealed class MetricLimitsApiTests(TelemetryApiFixture fixture)
{
    private readonly TelemetryApiFixture _fixture = fixture;

    [Theory]
    [MemberData(
        nameof(MetricLimitsApiTestData.InvalidCreateBodies),
        MemberType = typeof(MetricLimitsApiTestData)
    )]
    public async Task Apply_WithInvalidLimits_Returns400OnTheFieldKey(string caseName, string body, string key)
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(MetricDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(HttpMethod.Put, _fixture.DefinitionsPath(machineId), claims, body);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, caseName);
        text.Should().Contain(key, caseName);
    }

    [Fact]
    public async Task Apply_WithLimitsAndBehavior_ListReturnsThem()
    {
        var machineId = await _fixture.ArrangeMachineAsync();

        var claims = _fixture.CreateClaims(
            MetricDefinitionApiTestData.ReadPermission,
            MetricDefinitionApiTestData.WritePermission);

        var applied = await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricLimitsApiTestData.LimitsBody);

        var definition = await ReadSingleDefinitionAsync(machineId, claims);

        applied.StatusCode.Should().Be(HttpStatusCode.OK);
        definition["warningLow"]!.GetValue<double>().Should().Be(expected: 10);
        definition["warningHigh"]!.GetValue<double>().Should().Be(expected: 80);
        definition["alarmLow"]!.GetValue<double>().Should().Be(expected: 5);
        definition["alarmHigh"]!.GetValue<double>().Should().Be(expected: 90);
        definition["behavior"]!.GetValue<string>().Should().Be(expected: "counter");
    }

    [Fact]
    public async Task Apply_WithoutBehavior_StoresALevelMetric()
    {
        var machineId = await _fixture.ArrangeMachineAsync();

        var claims = _fixture.CreateClaims(
            MetricDefinitionApiTestData.ReadPermission,
            MetricDefinitionApiTestData.WritePermission);

        await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricLimitsApiTestData.LimitsWithoutBehaviorBody);

        var definition = await ReadSingleDefinitionAsync(machineId, claims);

        definition["behavior"]!.GetValue<string>().Should().Be(expected: "level");
        definition["alarmHigh"]!.GetValue<double>().Should().Be(expected: 90);
        definition["warningLow"].Should().BeNull();
    }

    [Fact]
    public async Task Apply_UpdateReplacesTheLimitsAndAnUpdateWithoutLimitsRemovesThem()
    {
        var machineId = await _fixture.ArrangeMachineAsync();

        var claims = _fixture.CreateClaims(
            MetricDefinitionApiTestData.ReadPermission,
            MetricDefinitionApiTestData.WritePermission);

        await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricLimitsApiTestData.LimitsBody);

        var id = (await ReadSingleDefinitionAsync(machineId, claims))["id"]!.GetValue<string>();

        await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricLimitsApiTestData.UpdateBodyTemplate.Replace(oldValue: "{id}", id));

        var replaced = await ReadSingleDefinitionAsync(machineId, claims);

        await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricLimitsApiTestData.UpdateWithoutLimitsBodyTemplate.Replace(oldValue: "{id}", id));

        var cleared = await ReadSingleDefinitionAsync(machineId, claims);

        replaced["warningHigh"]!.GetValue<double>().Should().Be(expected: 70);
        replaced["warningLow"].Should().BeNull();
        replaced["behavior"]!.GetValue<string>().Should().Be(expected: "level");
        cleared["warningHigh"].Should().BeNull();
        cleared["alarmHigh"].Should().BeNull();
    }

    [Fact]
    public async Task Apply_UpdateWithWarningLowBelowAlarmLow_Returns400OnTheFieldKey()
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

        var id = (await ReadSingleDefinitionAsync(machineId, claims))["id"]!.GetValue<string>();

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(machineId),
            claims,
            MetricLimitsApiTestData.UpdateWithBadOrderBodyTemplate.Replace(oldValue: "{id}", id));

        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        text.Should().Contain(expected: "updated[0].warningLow");
    }

    private async Task<JsonNode> ReadSingleDefinitionAsync(string machineId, TestTokenClaims claims)
    {
        var response = await _fixture.SendAsync(HttpMethod.Get, _fixture.DefinitionsPath(machineId), claims);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        return body["definitions"]!.AsArray().Single()!;
    }
}
