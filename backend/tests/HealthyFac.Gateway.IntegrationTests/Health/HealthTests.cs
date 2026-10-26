using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Gateway.IntegrationTests.Health;

public sealed class HealthTests : IAsyncLifetime
{
    private const string UnusedCoreAddress = "http://127.0.0.1:1/";
    private const string ClientAddress = "203.0.113.200";
    private const string LivenessRoute = "/health";
    private const string ReadinessRoute = "/health/ready";

    private readonly RedisTestServer _redis = new();

    public Task InitializeAsync() => _redis.StartAsync();

    public Task DisposeAsync() => _redis.DisposeAsync().AsTask();

    [Fact]
    public async Task HealthReady_WithRedisRunning_ReturnsHealthy()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();

        var response = await client.GetAsync(ReadinessRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReady_WhenRedisIsDown_ReturnsDegradedWith200()
    {
        await _redis.StopAsync();
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();

        var response = await client.GetAsync(ReadinessRoute);

        var status = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        status.Should().Be("Degraded");
    }

    [Fact]
    public async Task Health_WhenRedisIsDown_ReturnsOk()
    {
        await _redis.StopAsync();
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();

        var response = await client.GetAsync(LivenessRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private GatewayFactory CreateGateway() =>
        new(_redis.ConnectionString, UnusedCoreAddress, IPAddress.Parse(ClientAddress));
}
