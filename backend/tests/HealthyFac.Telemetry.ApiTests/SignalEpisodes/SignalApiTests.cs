using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.SignalEpisodes.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.ApiTests.SignalEpisodes;

[Collection(TelemetryApiCollection.Name)]
[Feature(TestFeatures.Evaluation)]
public sealed class SignalApiTests(TelemetryApiFixture fixture)
{
    private readonly TelemetryApiFixture _fixture = fixture;

    [Fact]
    public async Task MetricHistory_WithTheReadPermission_ReturnsTheBuckets()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var response = await _fixture.SendAsync(
            HttpMethod.Get,
            _fixture.MetricHistoryPath(arrangement, SignalApiTestData.BucketCount),
            _fixture.CreateClaims(SignalApiTestData.ReadPermission));

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body["buckets"]!.AsArray().Should().HaveCount(expected: 2);
        body["buckets"]![0]!["avg"]!.GetValue<double>().Should().Be(expected: 15);
        body["buckets"]![0]!["count"]!.GetValue<long>().Should().Be(expected: 2);
    }

    [Fact]
    public async Task SignalEpisodes_WithTheReadPermission_ReturnsTheEpisodesWithTheirSnapshots()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var response = await _fixture.SendAsync(
            HttpMethod.Get,
            _fixture.SignalEpisodesPath(arrangement),
            _fixture.CreateClaims(SignalApiTestData.ReadPermission));

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
        var episode = body["items"]![0]!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        episode["rule"]!.GetValue<string>().Should().Be(expected: "threshold");
        episode["metricKey"]!.GetValue<string>().Should().Be(SignalApiTestData.MetricKey);
        episode["value"]!.GetValue<double>().Should().Be(expected: 91);
        episode["phaseCode"]!.GetValue<string>().Should().Be(SignalApiTestData.PhaseCode);
    }

    [Fact]
    public async Task SignalRoutes_WithAnotherPermission_Return403()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();
        var claims = _fixture.CreateClaims(SignalApiTestData.OtherPermission);

        var history = await _fixture.SendAsync(
            HttpMethod.Get, _fixture.MetricHistoryPath(arrangement, SignalApiTestData.BucketCount), claims);

        var episodes = await _fixture.SendAsync(HttpMethod.Get, _fixture.SignalEpisodesPath(arrangement), claims);

        history.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        episodes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
