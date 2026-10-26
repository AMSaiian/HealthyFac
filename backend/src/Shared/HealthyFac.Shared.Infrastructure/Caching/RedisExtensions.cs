using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HealthyFac.Shared.Infrastructure.Caching;

/// <summary>
/// Provides extension methods for registering the shared Redis connection.
/// </summary>
public static class RedisExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated Redis options, one singleton <see cref="IConnectionMultiplexer"/>, and the
        /// <see cref="RedisCircuitBreaker"/>. The multiplexer does not abort when Redis is unreachable. The host
        /// then starts while Redis is down, and the connection recovers on its own when Redis returns. Connect
        /// and operation timeouts come from <see cref="RedisOptions"/>, so a call to a dead Redis fails fast.
        /// The breaker needs a registered <see cref="TimeProvider"/> (see <c>AddSharedTime</c>).
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedRedis(IConfiguration configuration)
        {
            services.AddValidatedOptions<RedisOptions, RedisOptionsValidator>(
                RedisOptions.SectionName,
                configuration);

            services.AddSingleton<RedisCircuitBreaker>();
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<RedisOptions>>().Value;
                var configurationOptions = ConfigurationOptions.Parse(options.ConnectionString);
                configurationOptions.AbortOnConnectFail = false;
                configurationOptions.ConnectTimeout = options.ConnectTimeoutMilliseconds;
                configurationOptions.SyncTimeout = options.OperationTimeoutMilliseconds;
                configurationOptions.AsyncTimeout = options.OperationTimeoutMilliseconds;

                return ConnectionMultiplexer.Connect(configurationOptions);
            });

            return services;
        }
    }
}
