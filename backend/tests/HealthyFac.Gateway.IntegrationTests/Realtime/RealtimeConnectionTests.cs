using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HealthyFac.Gateway.IntegrationTests.Health.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Realtime.Data;
using HealthyFac.Shared.Web.Realtime;
using Microsoft.Net.Http.Headers;

namespace HealthyFac.Gateway.IntegrationTests.Realtime;

[Collection(GatewayProxyCollection.Name)]
public sealed class RealtimeConnectionTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private const string UnusedCoreAddress = "http://127.0.0.1:1/";
    private const string ClosedRedis = "127.0.0.1:1";
    private const string ReadinessRoute = "/health/ready";
    private const string TransportNotSupported = "transport not supported by this end point type";
    private const string EventStream = "text/event-stream";

    private static readonly TimeSpan ShortLifetime = TimeSpan.FromSeconds(seconds: 3);
    private static readonly TimeSpan ErrorCloseTimeout = TimeSpan.FromSeconds(seconds: 5);
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(seconds: 15);

    private readonly GatewayEnvironment _environment = environment;

    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task Request_ForLongPolling_GetsNoConnection()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = CreateBrowserClient(gateway);

        var connectionToken = await NegotiateAsync(client);

        using var response = await client.GetAsync(
            $"{RealtimeConstants.HubPath}?id={connectionToken}",
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Should().Contain(TransportNotSupported);
    }

    [Fact]
    public async Task Request_ForServerSentEvents_GetsNoConnection()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = CreateBrowserClient(gateway);
        var connectionToken = await NegotiateAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{RealtimeConstants.HubPath}?id={connectionToken}");
        request.Headers.Accept.ParseAdd(EventStream);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Should().Contain(TransportNotSupported);
    }

    [Fact]
    public async Task Connection_WhenTokenExpires_ClosesWithinFifteenSeconds()
    {
        await using var gateway = _environment.CreateGateway(_core);
        var token = gateway.Tokens.CreateToken(
            RealtimeTestData.Claims(Guid.NewGuid()) with
            {
                Lifetime = ShortLifetime,
            });
        await using var client = await RealtimeTestClient.ConnectAsync(gateway, token);

        var isClosed = await client.WaitForCloseAsync(CloseTimeout);

        isClosed.Should().BeTrue();
    }

    [Fact]
    public async Task Connection_WithRedisDown_ClosesWithAnErrorAndReportsDegraded()
    {
        await using var gateway = new GatewayFactory(ClosedRedis, UnusedCoreAddress, _environment.NextClientAddress());
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()));

        await using var client = await RealtimeTestClient.ConnectAsync(gateway, token);
        var isClosed = await client.WaitForCloseAsync(ErrorCloseTimeout);
        var health = await gateway.GetOnHealthPortAsync(ReadinessRoute);

        isClosed.Should().BeTrue();
        client.CloseError.Should().NotBeNull();
        health.StatusCode.Should().Be(HttpStatusCode.OK);
        health.Body.Should().Be(expected: "Degraded");
    }

    private static async Task<string> NegotiateAsync(HttpClient client)
    {
        using var response = await client.PostAsync(
            $"{RealtimeConstants.HubPath}/negotiate?negotiateVersion=1",
            content: null,
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        return body.GetProperty(propertyName: "connectionToken").GetString()!;
    }

    private static HttpClient CreateBrowserClient(GatewayFactory gateway)
    {
        var client = gateway.CreateAuthenticatedClient(RealtimeTestData.Claims(Guid.NewGuid()));
        client.DefaultRequestHeaders.Add(HeaderNames.Origin, GatewayFactory.DefaultCorsOrigin);

        return client;
    }
}
