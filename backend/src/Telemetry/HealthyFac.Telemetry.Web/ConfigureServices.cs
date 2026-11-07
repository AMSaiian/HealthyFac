using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.Web.Authentication;
using HealthyFac.Shared.Web.CrossOrigin;
using HealthyFac.Shared.Web.CurrentUser;
using HealthyFac.Shared.Web.ErrorHandling;
using HealthyFac.Shared.Web.HealthChecks;
using HealthyFac.Shared.Web.Https;
using HealthyFac.Shared.Web.IdEncryption;
using HealthyFac.Shared.Web.Logging;
using HealthyFac.Shared.Web.Metrics;
using HealthyFac.Shared.Web.OpenApi;
using HealthyFac.Shared.Web.Proxy;
using HealthyFac.Shared.Web.Serialization;
using HealthyFac.Shared.Web.Tracing;
using HealthyFac.Shared.Web.Versioning;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;
using HealthyFac.Telemetry.Web.Authorization;
using HealthyFac.Telemetry.Web.MetricDefinitions;
using HealthyFac.Telemetry.Web.MetricUnits;
using HealthyFac.Telemetry.Web.StatusDefinitions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace HealthyFac.Telemetry.Web;

/// <summary>
/// Configures dependency injection, middleware pipeline, and endpoint routing for the web application.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers web API services, error handling, routing, and identifier encryption.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance containing
        /// settings.</param>
        /// <param name="environment">The host environment deciding which development-only surfaces load.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddApiServices(IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddApiNetworking(configuration);
            services.AddApiContracts(configuration);
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

            services.AddAuthorizationBuilder()
                .SetAuthenticatedFallbackPolicy()
                .AddPermissionPolicies(TelemetryPermissions.All);

            return services;
        }

        private IServiceCollection AddApiContracts(IConfiguration configuration)
        {
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
            });
            services.AddSharedJsonOptions();
            services.AddIdEncryption(configuration);
            services.AddSharedApiVersioning();
            services.AddSharedOpenApi();

            return services;
        }

        private IServiceCollection AddApiObservability(IConfiguration configuration)
        {
            services.AddSharedLogging(configuration);
            services.AddSharedTracing(configuration);
            services.AddSharedMetrics(configuration);

            services.AddSharedHealthChecks(configuration)
                .AddDbContextCheck<TelemetryDbContext>(
                    DatabaseConstants.HealthCheckName,
                    tags: [HealthCheckConstants.ReadyTag])
                .AddBrokerHealthCheck([HealthCheckConstants.ReadyTag])
                .AddCheck<MqttIntakeHealthCheck>(
                    MqttIntakeConstants.HealthCheckName,
                    HealthStatus.Degraded,
                    [HealthCheckConstants.ReadyTag]);

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Configures the HTTP request pipeline and middleware components.
        /// </summary>
        /// <remarks>
        /// The request log middleware wraps the exception handler, so the log records the status that the client
        /// received. Placed inside the handler, it would log 500 for a response that the handler rewrites to 400.
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
            app.MapSharedOpenApi();
            var versionedGroup = app.MapVersionedGroup();
            versionedGroup.MapMetricUnitEndpoints();
            versionedGroup.MapMetricDefinitionEndpoints();
            versionedGroup.MapStatusDefinitionEndpoints();

            return app;
        }
    }
}
