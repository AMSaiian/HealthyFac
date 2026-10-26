using HealthyFac.Gateway.Web.Proxy;
using HealthyFac.Gateway.Web.RateLimiting;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.Time;
using HealthyFac.Shared.Web.HealthChecks;
using HealthyFac.Shared.Web.Logging;
using HealthyFac.Shared.Web.Proxy;
using HealthyFac.Shared.Web.Tracing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using StackExchange.Redis;
using IAvailableDestinationsPolicy = Yarp.ReverseProxy.Health.IAvailableDestinationsPolicy;

namespace HealthyFac.Gateway.Web;

/// <summary>
/// Configures dependency injection, middleware pipeline, and endpoint routing for the gateway.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the reverse proxy, the Redis connection, rate limiting, error handling, and observability.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance containing
        /// settings.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddGatewayServices(IConfiguration configuration)
        {
            services.AddSharedTime();
            services.AddSharedForwardedHeaders(configuration);
            services.AddExceptionHandler<GatewayExceptionHandler>();
            services.AddProblemDetails();
            services.AddSharedRedis(configuration);
            services.AddGatewayRateLimiting(configuration);
            services.AddReverseProxy().LoadFromConfig(configuration.GetSection(GatewayConstants.ReverseProxySection));
            services.AddSingleton<IAvailableDestinationsPolicy, HealthyOnlyDestinationsPolicy>();
            services.AddSingleton<ProxyErrorMiddleware>();
            services.AddGatewayObservability(configuration);

            return services;
        }

        private IServiceCollection AddGatewayObservability(IConfiguration configuration)
        {
            services.AddSharedLogging(configuration);
            services.AddSharedTracing(configuration);
            services.AddSharedHealthChecks()
                .AddRedis(
                    provider => provider.GetRequiredService<IConnectionMultiplexer>(),
                    GatewayConstants.RedisHealthCheckName,
                    failureStatus: HealthStatus.Degraded,
                    tags: [HealthCheckConstants.ReadyTag]);

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Configures the HTTP request pipeline and middleware components.
        /// </summary>
        /// <returns>The configured <see cref="WebApplication"/> instance.</returns>
        public WebApplication UseGatewayPipeline()
        {
            app.UseSharedForwardedHeaders();
            app.UseSerilogRequestLogging();
            app.UseExceptionHandler();
            app.UseRateLimiter();

            return app;
        }
    }

    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the health endpoints and the reverse proxy routes.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapGatewayEndpoints()
        {
            app.MapSharedHealthChecks();
            app.MapReverseProxy(proxy =>
            {
                proxy.UseMiddleware<ProxyErrorMiddleware>();
                proxy.UseLoadBalancing();
                proxy.UsePassiveHealthChecks();
            });

            return app;
        }
    }
}
