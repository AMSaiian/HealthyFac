using System.Text.Json.Nodes;
using HealthyFac.Gateway.IntegrationTests.Composition.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.Web.Composition;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Gateway.IntegrationTests.Composition;

[Collection(GatewayProxyCollection.Name)]
public sealed class DownstreamClientTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;
    private readonly StubApiServer _core = new();
    private readonly StubApiServer _telemetry = new();

    public async ValueTask InitializeAsync()
    {
        await _core.StartAsync();
        await _telemetry.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _core.DisposeAsync();
        await _telemetry.DisposeAsync();
    }

    [Fact]
    public async Task Send_WithChangedClusterAddresses_ReachesTheChangedServers()
    {
        _core.Respond(HttpMethod.Get, CompositionTestData.Path, status: 200, CompositionTestData.OkJson);
        _telemetry.Respond(HttpMethod.Get, CompositionTestData.Path, status: 200, CompositionTestData.OkJson);
        await using var gateway = CreateGateway();

        var coreResult = await SendAsync(gateway, ComposedClusters.Core);
        var telemetryResult = await SendAsync(gateway, ComposedClusters.Telemetry);

        coreResult.Should().BeOfType<DownstreamResult.Success>();
        telemetryResult.Should().BeOfType<DownstreamResult.Success>();
        _core.Requests.Should().ContainSingle();
        _telemetry.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Send_ToAServer_ForwardsAuthorizationAndTraceAndNoActorHeader()
    {
        _core.Respond(HttpMethod.Get, CompositionTestData.Path, status: 200, CompositionTestData.OkJson);
        await using var gateway = CreateGateway();

        await SendAsync(gateway, ComposedClusters.Core);

        var forwarded = _core.Requests.Single();

        forwarded.Headers[CompositionTestData.TraceParentHeader].Should().NotBeNullOrEmpty();

        forwarded.Headers.Should()
            .ContainKey(expected: "Authorization")
            .WhoseValue.Should()
            .Be(CompositionTestData.BearerToken);

        forwarded.Headers.Should().NotContainKey(CompositionTestData.ActorIdHeader);
        forwarded.Headers.Should().NotContainKey(CompositionTestData.CompanyIdHeader);
    }

    [Fact]
    public async Task Send_ToAStoppedServer_ReturnsUnavailable()
    {
        await using var gateway = CreateGateway();
        await _core.DisposeAsync();

        var result = await SendAsync(gateway, ComposedClusters.Core);

        result.Should().BeOfType<DownstreamResult.Unavailable>();
    }

    [Fact]
    public async Task Send_ToAServerThatAnswers404_ReturnsClientErrorWithTheProblem()
    {
        _core.Respond(HttpMethod.Get, CompositionTestData.Path, status: 404, CompositionTestData.ProblemJson);
        await using var gateway = CreateGateway();

        var result = await SendAsync(gateway, ComposedClusters.Core);

        var error = result.Should().BeOfType<DownstreamResult.ClientError>().Subject;

        error.Status.Should().Be(expected: 404);
        error.Problem["title"]!.GetValue<string>().Should().Be(expected: "Not found");
    }

    private GatewayFactory CreateGateway() =>
        _environment.CreateGateway(
            _core.Address,
            settings: new Dictionary<string, string>
            {
                [GatewaySettingKeys.TelemetryAddress] = _telemetry.Address,
                [GatewaySettingKeys.TelemetryHealthAddress] = _telemetry.Address,
                [GatewaySettingKeys.TracingEnabled] = bool.TrueString,
            });

    private static async Task<DownstreamResult> SendAsync(GatewayFactory gateway, string clusterId)
    {
        var incoming = new DefaultHttpContext();
        incoming.Request.Headers.Authorization = CompositionTestData.BearerToken;
        incoming.Request.Headers[CompositionTestData.ActorIdHeader] = Guid.NewGuid().ToString();
        incoming.Request.Headers[CompositionTestData.CompanyIdHeader] = Guid.NewGuid().ToString();
        _ = gateway.CreateClient();

        return await gateway.Services
            .GetRequiredService<DownstreamClient>()
            .SendAsync(
                clusterId,
                HttpMethod.Get,
                CompositionTestData.Path,
                (JsonNode?)null,
                incoming,
                TestContext.Current.CancellationToken);
    }
}
