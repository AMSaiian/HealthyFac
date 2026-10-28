using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Revocation.Data;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.Revocation;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Gateway.IntegrationTests.Revocation;

public sealed class RevocationRedisOutageTests(GatewayEnvironment environment)
    : IClassFixture<GatewayEnvironment>, IAsyncLifetime
{
    private const string DownClientAddress = "203.0.113.200";

    private readonly EchoServer _core = new(useForwardedHeaders: true);
    private readonly RedisTestServer _stoppedRedis = new();

    public async Task InitializeAsync()
    {
        await _core.StartAsync();
        await _stoppedRedis.StartAsync();
        await _stoppedRedis.StopAsync();
    }

    public async Task DisposeAsync()
    {
        await _core.DisposeAsync();
        await _stoppedRedis.DisposeAsync();
    }

    [Fact]
    public async Task Revocation_WhenRedisIsDown_PassesAndLogsWarning()
    {
        var breakerLogger = new CapturingLogger<RedisCircuitBreaker>();
        await using var gateway = new GatewayFactory(
            _stoppedRedis.ConnectionString,
            _core.Address,
            System.Net.IPAddress.Parse(DownClientAddress),
            configureServices: services => ReplaceBreaker(services, new FakeTimeProvider(), breakerLogger));
        using var client = gateway.CreateAuthenticatedClient();

        using var response = await client.GetAsync(RevocationTestData.CoreRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        breakerLogger.Entries.Should().Contain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Revocation_AfterRedisRecovers_ReadsAgain()
    {
        var store = new FlakyRevocationStore();
        var time = new FakeTimeProvider();
        await using var gateway = environment.CreateGateway(
            _core,
            configureServices: services =>
            {
                services.Replace(ServiceDescriptor.Singleton<ITokenRevocationStore>(store));
                ReplaceBreaker(services, time, new CapturingLogger<RedisCircuitBreaker>());
            });
        using var client = gateway.CreateAuthenticatedClient();
        using var failedRead = await client.GetAsync(RevocationTestData.CoreRoute);
        using var skippedRead = await client.GetAsync(RevocationTestData.CoreRoute);
        var readsWhileOpen = store.ReadCount;
        time.Advance(RedisConstants.OpenPeriod);
        using var probeRead = await client.GetAsync(RevocationTestData.CoreRoute);

        using var response = await client.GetAsync(RevocationTestData.CoreRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        readsWhileOpen.Should().Be(1);
        store.ReadCount.Should().Be(3);
    }

    private static void ReplaceBreaker(
        IServiceCollection services,
        FakeTimeProvider time,
        ILogger<RedisCircuitBreaker> logger
    ) => services.Replace(ServiceDescriptor.Singleton(new RedisCircuitBreaker(time, logger)));
}
