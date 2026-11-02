using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthyFac.Shared.Infrastructure.Caching;

public static class RedisBreakerHealthCheckExtensions
{
    extension(IHealthChecksBuilder builder)
    {
        /// <summary>
        /// Adds the check that reports the state of the Redis breaker. It reports
        /// <see cref="HealthStatus.Degraded"/> while the breaker is not closed. The service must call
        /// <c>AddSharedRedis</c> first. The check has its own short timeout, because it can ping Redis.
        /// </summary>
        /// <param name="tags">The tags of the check.</param>
        /// <returns>The updated <see cref="IHealthChecksBuilder"/> instance for method chaining.</returns>
        public IHealthChecksBuilder AddRedisBreakerHealthCheck(IEnumerable<string> tags) =>
            builder.AddCheck<RedisBreakerHealthCheck>(
                RedisConstants.Resilience.HealthCheckName,
                HealthStatus.Degraded,
                tags,
                RedisConstants.Resilience.HealthCheckTimeout);
    }
}
