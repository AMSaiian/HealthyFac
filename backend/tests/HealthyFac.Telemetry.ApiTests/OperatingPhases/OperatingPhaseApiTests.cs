using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.OperatingPhases.Data;

namespace HealthyFac.Telemetry.ApiTests.OperatingPhases;

[Collection(TelemetryApiCollection.Name)]
public sealed class OperatingPhaseApiTests(TelemetryApiFixture fixture)
{
    private readonly TelemetryApiFixture _fixture = fixture;

    [Fact]
    public async Task Phases_RoundTripOverHttp_WorksForEachRoute()
    {
        var arranged = await _fixture.ArrangePhaseMachineAsync();
        var path = _fixture.OperatingPhasesPath(arranged.MachineId);
        var write = _fixture.CreateClaims(OperatingPhaseApiTestData.WritePermission);
        var read = _fixture.CreateClaims(OperatingPhaseApiTestData.ReadPermission);

        var created = await _fixture.SendAsync(
            HttpMethod.Post,
            path,
            write,
            _fixture.OperatingPhaseBody(OperatingPhaseApiTestData.Code, arranged.Id, priority: 0));

        var location = created.Headers.Location!.ToString();
        var phaseId = location.Split(separator: '/')[^1];

        var fetched = await _fixture.SendAsync(HttpMethod.Get, $"{path}/{phaseId}", read);
        var listed = await _fixture.SendAsync(HttpMethod.Get, path, read);

        var updated = await _fixture.SendAsync(
            HttpMethod.Put,
            $"{path}/{phaseId}",
            write,
            _fixture.OperatingPhaseBody(code: "stopped", arranged.Id, priority: 4));

        var reordered = await _fixture.SendAsync(
            HttpMethod.Put,
            $"{path}/{OperatingPhaseApiTestData.OrderSegment}",
            write,
            new JsonObject { ["phaseIds"] = new JsonArray(phaseId) }.ToJsonString());

        var deleted = await _fixture.SendAsync(HttpMethod.Delete, $"{path}/{phaseId}", write);
        var afterDelete = await _fixture.SendAsync(HttpMethod.Get, $"{path}/{phaseId}", read);
        var fetchedBody = await fetched.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var listedBody = await listed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        fetchedBody.Should().Contain($"\"code\":\"{OperatingPhaseApiTestData.Code}\"");
        listed.StatusCode.Should().Be(HttpStatusCode.OK);
        listedBody.Should().Contain($"\"code\":\"{OperatingPhaseApiTestData.Code}\"");
        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        reordered.StatusCode.Should().Be(HttpStatusCode.NoContent);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [MemberData(
        nameof(OperatingPhaseApiTestData.RoutesWithOtherPermission),
        MemberType = typeof(OperatingPhaseApiTestData))]
    public async Task Route_WithTheOtherPermission_Returns403(
        string caseName,
        string method,
        string suffix,
        string permission,
        string? body
    )
    {
        var arranged = await _fixture.ArrangePhaseMachineAsync();
        var claims = _fixture.CreateClaims(permission);

        var response = await _fixture.SendAsync(
            new HttpMethod(method),
            _fixture.OperatingPhasesPath(arranged.MachineId) + suffix,
            claims,
            body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, caseName);
    }
}
