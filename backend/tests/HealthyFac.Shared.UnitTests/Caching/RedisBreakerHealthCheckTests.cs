using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.Caching.BreakerHealth;
using HealthyFac.Shared.UnitTests.Caching.Data;
using HealthyFac.Shared.UnitTests.ServiceTokens.Data;
using HealthyFac.Testing.Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Shared.UnitTests.Caching;

public sealed class RedisBreakerHealthCheckTests
{
    private readonly FakeTimeProvider _time = new();
    private readonly RedisResiliencePipeline _redis;

    public RedisBreakerHealthCheckTests()
    {
        _redis = TestRedisPipeline.Create(_time);
    }

    [Fact]
    public async Task RedisBreakerHealthCheck_WhenBreakerOpen_ReportsDegraded()
    {
        var (connection, counter) = CallCountingConnection.Create();
        var healthCheck = new RedisBreakerHealthCheck(_redis, connection);
        await _redis.OpenBreakerAsync();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        counter.CallCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task RedisBreakerHealthCheck_WhenBreakPassedAndRedisBack_ReportsHealthy()
    {
        var healthCheck = new RedisBreakerHealthCheck(_redis, PingAnsweringConnection.Create());
        await _redis.OpenBreakerAsync();
        _time.Advance(RedisConstants.Resilience.BreakDuration);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task RedisBreakerHealthCheck_WhenBreakerClosed_ReportsHealthy()
    {
        var (connection, counter) = CallCountingConnection.Create();
        var healthCheck = new RedisBreakerHealthCheck(_redis, connection);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        counter.CallCount.Should().Be(expected: 0);
    }
}
