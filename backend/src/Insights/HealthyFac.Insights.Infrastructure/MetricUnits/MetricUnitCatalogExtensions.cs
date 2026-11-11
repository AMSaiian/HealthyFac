using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.Infrastructure.MetricUnits;

/// <summary>
/// Provides extension methods for registering the unit catalog.
/// </summary>
public static class MetricUnitCatalogExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated cache options, the Telemetry source, and the Redis catalog, which serves both the
        /// catalog and the cache interface. The service must call <c>AddSharedRedis</c> and <c>AddTelemetryClient</c>.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddMetricUnitCatalog(IConfiguration configuration)
        {
            services.AddValidatedOptions<MetricUnitCacheOptions, MetricUnitCacheOptionsValidator>(
                MetricUnitCacheOptions.SectionName,
                configuration);

            services.AddSingleton<IMetricUnitSource, TelemetryMetricUnitClient>();
            services.AddSingleton<RedisMetricUnitCatalog>();
            services.AddSingleton<IMetricUnitCatalog>(provider =>
                provider.GetRequiredService<RedisMetricUnitCatalog>());
            services.AddSingleton<IMetricUnitCache>(provider => provider.GetRequiredService<RedisMetricUnitCatalog>());

            return services;
        }
    }
}
