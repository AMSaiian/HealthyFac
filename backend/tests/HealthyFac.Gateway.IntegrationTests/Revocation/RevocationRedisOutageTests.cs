using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Revocation.Data;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.Revocation;
using HealthyFac.Testing.Caching;
using HealthyFac.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Gateway.IntegrationTests.Revocation;

[Collection(GatewayLimitsCollection.Name)]
public sealed class RevocationRedisOutageTests(GatewayEnvironment environment)
    : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;

    private const string DownClientAddress = "203.0.113.200";

    private readonly EchoServer _core = new(useForwardedHeaders: true);
    private readonly RedisTestServer _stoppedRedis = new();

    public async ValueTask InitializeAsync()
    {
        await _core.StartAsync();
        await _stoppedRedis.StartAsync();
        await _stoppedRedis.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _core.DisposeAsync();
        await _stoppedRedis.DisposeAsync();
    }

    [Fact]
    public async Task Revocation_WhenRedisIsDown_Passes()
    {
        await using var gateway = new GatewayFactory(
            _stoppedRedis.ConnectionString,
            _core.Address,
            IPAddress.Parse(DownClientAddress));
        using var client = gateway.CreateAuthenticatedClient();
        using var firstFailure = await client.GetAsync(
            RevocationTestData.CoreRoute, TestContext.Current.CancellationToken);

        using var response = await client.GetAsync(RevocationTestData.CoreRoute, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Revocation_AfterRedisRecovers_ReadsAgain()
    {
        var store = new FlakyRevocationStore();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await using var gateway = _environment.CreateGateway(
            _core,
            configureServices: services =>
            {
                services.Replace(ServiceDescriptor.Singleton<ITokenRevocationStore>(store));
                services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
            });
        using var client = gateway.CreateAuthenticatedClient();
        await gateway.Services.GetRequiredService<RedisResiliencePipeline>()
            .FailAsync(RedisConstants.Resilience.MinimumThroughput - 1);
        using var failedRead = await client.GetAsync(
            RevocationTestData.CoreRoute, TestContext.Current.CancellationToken);
        using var skippedRead = await client.GetAsync(
            RevocationTestData.CoreRoute, TestContext.Current.CancellationToken);
        var readsWhileOpen = store.ReadCount;
        time.Advance(RedisConstants.Resilience.BreakDuration);
        using var probeRead = await client.GetAsync(
            RevocationTestData.CoreRoute, TestContext.Current.CancellationToken);

        using var response = await client.GetAsync(RevocationTestData.CoreRoute, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        readsWhileOpen.Should().Be(expected: 1);
        store.ReadCount.Should().Be(expected: 3);
    }
}
