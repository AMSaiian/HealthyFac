using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.Infrastructure.Caching;

/// <summary>
/// Provides extension methods for registering the Insights caches.
/// </summary>
public static class InsightsCacheExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated options of the Insights caches and the shared Redis fetch cache. The service must
        /// call <c>AddSharedRedis</c>.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddInsightsCaches(IConfiguration configuration)
        {
            services.AddValidatedOptions<InsightsCacheOptions, InsightsCacheOptionsValidator>(
                InsightsCacheOptions.SectionName,
                configuration);

            services.AddSingleton<RedisFetchCache>();

            return services;
        }
    }
}
