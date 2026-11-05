using HealthyFac.Auth.Infrastructure.Database;
using HealthyFac.Shared.Infrastructure.Caching.BreakerHealth;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.Web.HealthChecks;
using HealthyFac.Shared.Web.Logging;
using HealthyFac.Shared.Web.Metrics;
using HealthyFac.Shared.Web.Tracing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HealthyFac.Auth.Web;

/// <summary>
/// Configures dependency injection and the middleware pipeline for the host of the auth service. The service maps
/// no endpoint, so the host serves the health endpoints only. It registers no authentication and no authorization.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers logging, tracing, and the health checks of the database and the message broker.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance containing
        /// settings.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddApiServices(IConfiguration configuration)
        {
            services.AddSharedLogging(configuration);
            services.AddSharedTracing(configuration);
            services.AddSharedMetrics(configuration);

            services.AddSharedHealthChecks(configuration)
                .AddDbContextCheck<AuthDbContext>(
                    DatabaseConstants.HealthCheckName,
                    tags: [HealthCheckConstants.ReadyTag])
                .AddBrokerHealthCheck([HealthCheckConstants.ReadyTag])
                .AddRedis(
                    provider => provider.GetRequiredService<IConnectionMultiplexer>(),
                    HealthCheckConstants.RedisCheckName,
                    HealthStatus.Degraded,
                    [HealthCheckConstants.ReadyTag])
                .AddRedisBreakerHealthCheck([HealthCheckConstants.ReadyTag]);

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Configures the HTTP request pipeline. The health middleware is the only component.
        /// </summary>
        /// <returns>The configured <see cref="WebApplication"/> instance.</returns>
        public WebApplication UseApiPipeline()
        {
            app.UseSharedHealthChecks();

            return app;
        }
    }
}
