using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace HealthyFac.Shared.Infrastructure.Caching.BreakerHealth;

/// <summary>
/// Reports <see cref="HealthStatus.Degraded"/> while the Redis breaker is not closed, and never
/// <see cref="HealthStatus.Unhealthy"/>: the service works on without Redis, and an unhealthy readiness status would
/// take it out of the gateway.
/// </summary>
/// <remarks>
/// The breaker leaves the open state only when a call comes. A service with few Redis calls would then report
/// <see cref="HealthStatus.Degraded"/> long after Redis returns. So while the breaker is not closed, the check sends
/// 1 <c>PING</c> through the pipeline. During the break period the ping fails at once and does not reach Redis. After
/// the break period the ping is the probe that closes the breaker. While the breaker is closed, the check sends
/// nothing.
/// </remarks>
/// <param name="redis">The shared Redis pipeline.</param>
/// <param name="connection">The shared Redis connection.</param>
internal sealed class RedisBreakerHealthCheck(RedisResiliencePipeline redis, IConnectionMultiplexer connection)
    : IHealthCheck
{
    private readonly RedisResiliencePipeline _redis = redis;
    private readonly IConnectionMultiplexer _connection = connection;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (_redis.State != CircuitState.Closed)
        {
            await TryPingAsync(cancellationToken);
        }

        var state = _redis.State;

        return state == CircuitState.Closed
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Degraded($"The Redis circuit breaker is {state}. Redis calls fail fast.");
    }

    private async Task TryPingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _redis.ExecuteAsync(
                token => new ValueTask<TimeSpan>(_connection.GetDatabase().PingAsync().WaitAsync(token)),
                cancellationToken);
        }
        catch (Exception exception)
            when (exception is BrokenCircuitException or RedisException or RedisTimeoutException)
        {
        }
    }
}
