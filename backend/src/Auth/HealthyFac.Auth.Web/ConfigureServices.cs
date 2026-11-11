using HealthyFac.Auth.Infrastructure.Database;
using HealthyFac.Auth.Web.Locales;
using HealthyFac.Shared.Infrastructure.Caching.BreakerHealth;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.Web.Authentication;
using HealthyFac.Shared.Web.CrossOrigin;
using HealthyFac.Shared.Web.CurrentUser;
using HealthyFac.Shared.Web.ErrorHandling;
using HealthyFac.Shared.Web.HealthChecks;
using HealthyFac.Shared.Web.Https;
using HealthyFac.Shared.Web.Logging;
using HealthyFac.Shared.Web.Metrics;
using HealthyFac.Shared.Web.Proxy;
using HealthyFac.Shared.Web.Serialization;
using HealthyFac.Shared.Web.Tracing;
using HealthyFac.Shared.Web.Versioning;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using StackExchange.Redis;

namespace HealthyFac.Auth.Web;

/// <summary>
/// Configures dependency injection and the middleware pipeline for the host of the auth service. The service maps one
/// endpoint group (the locale of the current user) and the health endpoints.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers logging, tracing, security, the health checks, and the JSON and versioning settings.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance containing
        /// settings.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddApiServices(IConfiguration configuration)
        {
            services.AddApiNetworking(configuration);
            services.AddApiContracts();
            services.AddApiObservability(configuration);
            services.AddApiSecurity(configuration);

            services.AddActorContextMiddleware();
            services.AddSharedExceptionHandling();

            return services;
        }

        private IServiceCollection AddApiNetworking(IConfiguration configuration)
        {
            services.AddSharedForwardedHeaders(configuration);
            services.AddSharedCors(configuration);
            services.AddSharedHttps(configuration);

            return services;
        }

        private IServiceCollection AddApiSecurity(IConfiguration configuration)
        {
            services.AddSharedJwtAuthentication(configuration);
            services.AddAuthorizationBuilder().SetAuthenticatedFallbackPolicy();

            return services;
        }

        private IServiceCollection AddApiContracts()
        {
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
            });
            services.AddSharedJsonOptions();
            services.AddSharedApiVersioning();

            return services;
        }

        private IServiceCollection AddApiObservability(IConfiguration configuration)
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
        /// Configures the HTTP request pipeline.
        /// </summary>
        /// <remarks>
        /// The request log middleware wraps the exception handler, so the log records the status that the client
        /// received.
        /// </remarks>
        /// <returns>The configured <see cref="WebApplication"/> instance.</returns>
        public WebApplication UseApiPipeline()
        {
            app.UseSharedHealthChecks();
            app.UseSharedForwardedHeaders();
            app.UseSharedHttps();
            app.UseSerilogRequestLogging();

            app.UseSharedCors();
            app.UseSharedExceptionHandling();
            app.UseAuthentication();
            app.UseActorContext();
            app.UseAuthorization();

            return app;
        }
    }

    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all web API endpoints to the route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapApiEndpoints()
        {
            var versionedGroup = app.MapVersionedGroup();
            versionedGroup.MapLocaleEndpoints();

            return app;
        }
    }
}
