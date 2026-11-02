using HealthyFac.Shared.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace HealthyFac.Testing.Caching;

/// <summary>
/// Builds the shared Redis pipeline through the production registration of <c>AddSharedRedis</c>, so a test uses the
/// same breaker options. The connection string points at no server; the pipeline never connects by itself.
/// </summary>
public static class TestRedisPipeline
{
    private const string UnusedConnectionString = "localhost:1";
    private const string SimulatedFailure = "Simulated Redis failure.";

    /// <summary>
    /// Creates the pipeline with its own service provider.
    /// </summary>
    /// <param name="timeProvider">The clock of the breaker, usually a <c>FakeTimeProvider</c>.</param>
    /// <returns>The pipeline with a closed breaker.</returns>
    public static RedisResiliencePipeline Create(TimeProvider timeProvider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{RedisOptions.SectionName}:ConnectionString"] = UnusedConnectionString,
                })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(timeProvider);
        services.AddSharedRedis(configuration);

        return services.BuildServiceProvider().GetRequiredService<RedisResiliencePipeline>();
    }

    extension(RedisResiliencePipeline pipeline)
    {
        /// <summary>
        /// Opens the breaker by real failures: as many failed Redis calls as the breaker needs.
        /// </summary>
        /// <returns>A task that completes when the breaker is open.</returns>
        public Task OpenBreakerAsync() => pipeline.FailAsync(RedisConstants.Resilience.MinimumThroughput);

        /// <summary>
        /// Runs Redis calls that fail with a <see cref="RedisException"/> through the pipeline.
        /// </summary>
        /// <param name="count">The number of failed calls.</param>
        /// <returns>A task that completes after the last call.</returns>
        public async Task FailAsync(int count)
        {
            for (var index = 0; index < count; index++)
            {
                try
                {
                    await pipeline.ExecuteAsync(
                        _ => ValueTask.FromException<bool>(new RedisException(SimulatedFailure)),
                        CancellationToken.None);
                }
                catch (Exception exception) when (exception is RedisException or BrokenCircuitException)
                {
                }
            }
        }
    }
}
