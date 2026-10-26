using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting;

public sealed class RateLimitRedisOutageTests : IAsyncLifetime
{
    private const string ClientAddress = "203.0.113.150";

    private readonly RedisTestServer _redis = new();

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        await _redis.StopAsync();
    }

    public Task DisposeAsync() => _redis.DisposeAsync().AsTask();

    [Fact]
    public async Task StrictPolicy_WhenRedisIsDown_FallsBackToLocalLimit()
    {
        await using var core = new EchoServer();
        await core.StartAsync();
        await using var gateway = CreateGateway(core, RateLimitTestData.StrictLimitOfThree);
        using var client = gateway.CreateClient();
        await client.SpendStrictLimitAsync();

        using var response = await client.PostVerifyInviteAsync();

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        core.RequestCount.Should().Be(RateLimitTestData.StrictPermitLimit);
    }

    [Fact]
    public async Task DefaultPolicy_WhenRedisIsDown_NeverReturns500()
    {
        await using var core = new EchoServer();
        await core.StartAsync();
        await using var gateway = CreateGateway(core);
        using var client = gateway.CreateClient();

        var statusCodes = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < RateLimitTestData.DefaultRequestsDuringOutage; attempt++)
        {
            using var response = await client.GetAsync(RateLimitTestData.MachinesRoute);
            statusCodes.Add(response.StatusCode);
        }

        statusCodes.Should().OnlyContain(statusCode => statusCode == HttpStatusCode.OK);
        core.RequestCount.Should().Be(RateLimitTestData.DefaultRequestsDuringOutage);
    }

    private GatewayFactory CreateGateway(EchoServer core, IReadOnlyDictionary<string, string>? settings = null) =>
        new(_redis.ConnectionString, core.Address, IPAddress.Parse(ClientAddress), settings);
}
