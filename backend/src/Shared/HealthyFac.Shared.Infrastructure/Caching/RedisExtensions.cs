using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
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
        /// Registers the validated Redis options, one singleton <see cref="IConnectionMultiplexer"/>, and the shared
        /// Redis pipeline (<see cref="RedisResiliencePipeline"/>) with its circuit breaker. The multiplexer does not
        /// abort when Redis is unreachable. The host then starts while Redis is down, and the connection recovers on
        /// its own when Redis returns. Connect and operation timeouts come from <see cref="RedisOptions"/>, so a call
        /// to a dead Redis fails fast, and the open breaker fails every call at once. The breaker reads the
        /// registered <see cref="TimeProvider"/> (see <c>AddSharedTime</c>), so a fake clock drives it in tests. A
        /// second call registers nothing new.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedRedis(IConfiguration configuration)
        {
            var isRegistered = services.Any(descriptor => descriptor.ServiceType == typeof(RedisResiliencePipeline));
            if (isRegistered)
            {
                return services;
            }

            services.AddValidatedOptions<RedisOptions, RedisOptionsValidator>(
                RedisOptions.SectionName,
                configuration);

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

            services.AddRedisResiliencePipeline();

            return services;
        }

        private void AddRedisResiliencePipeline()
        {
            services.AddKeyedSingleton<CircuitBreakerStateProvider>(RedisConstants.Resilience.PipelineKey);
            services.AddSingleton<RedisResiliencePipeline>();
            services.AddResiliencePipeline(
                RedisConstants.Resilience.PipelineKey,
                (builder, context) =>
                {
                    var provider = context.ServiceProvider;

                    builder.TimeProvider = provider.GetRequiredService<TimeProvider>();
                    builder.AddCircuitBreaker(
                        new CircuitBreakerStrategyOptions
                        {
                            ShouldHandle = new PredicateBuilder()
                                .Handle<RedisException>()
                                .Handle<RedisTimeoutException>(),
                            FailureRatio = RedisConstants.Resilience.FailureRatio,
                            MinimumThroughput = RedisConstants.Resilience.MinimumThroughput,
                            SamplingDuration = RedisConstants.Resilience.SamplingDuration,
                            BreakDuration = RedisConstants.Resilience.BreakDuration,
                            StateProvider = provider.GetRequiredKeyedService<CircuitBreakerStateProvider>(
                                RedisConstants.Resilience.PipelineKey),
                        });
                });
        }
    }
}
