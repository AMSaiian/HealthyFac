using System.Net;
using HealthyFac.Gateway.IntegrationTests.Health.Data;
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

    public async ValueTask InitializeAsync() => await _redis.StartAsync();

    public ValueTask DisposeAsync() => _redis.DisposeAsync();

    [Fact]
    public async Task HealthReady_WithRedisRunning_ReturnsHealthy()
    {
        await using var gateway = CreateGateway();

        var response = await gateway.GetOnHealthPortAsync(ReadinessRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReady_WhenRedisIsDown_ReturnsDegradedWith200()
    {
        await _redis.StopAsync();
        await using var gateway = CreateGateway();

        var response = await gateway.GetOnHealthPortAsync(ReadinessRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Should().Be(expected: "Degraded");
    }

    [Fact]
    public async Task Health_WhenRedisIsDown_ReturnsOk()
    {
        await _redis.StopAsync();
        await using var gateway = CreateGateway();

        var response = await gateway.GetOnHealthPortAsync(LivenessRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private GatewayFactory CreateGateway() =>
        new(_redis.ConnectionString, UnusedCoreAddress, IPAddress.Parse(ClientAddress));
}
