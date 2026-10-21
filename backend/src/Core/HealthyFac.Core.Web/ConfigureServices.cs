using Serilog;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Web.Machines;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Web.CrossOrigin;
using HealthyFac.Shared.Web.ErrorHandling;
using HealthyFac.Shared.Web.Rpc;
using HealthyFac.Shared.Web.Serialization;
using HealthyFac.Shared.Web.Tracing;
using HealthyFac.Shared.Web.HealthChecks;
using HealthyFac.Shared.Web.Https;
using HealthyFac.Shared.Web.IdEncryption;
using HealthyFac.Shared.Web.Logging;
using HealthyFac.Shared.Web.OpenApi;
using HealthyFac.Shared.Web.CurrentUser;
using HealthyFac.Shared.Web.Proxy;
using HealthyFac.Shared.Web.Versioning;

namespace HealthyFac.Core.Web;

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

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
            services.AddSharedExceptionHandling();
            services.AddSharedGrpc(environment);

            return services;
        }

        private IServiceCollection AddApiNetworking(IConfiguration configuration)
        {
            services.AddSharedForwardedHeaders(configuration);
            services.AddSharedCors(configuration);
            services.AddSharedHttps(configuration);

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
            services.AddSharedHealthChecks()
                .AddDbContextCheck<CoreDbContext>(
                    DatabaseConstants.HealthCheckName,
                    tags: [HealthCheckConstants.ReadyTag]);

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
            app.UseSharedForwardedHeaders();
            app.UseSharedHttps();
            app.UseSerilogRequestLogging();

            app.UseSharedCors();
            app.UseSharedExceptionHandling();

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
            app.MapApiDiagnostics();

            var versionedGroup = app.MapVersionedGroup();
            versionedGroup.MapMachineEndpoints();

            return app;
        }

        private IEndpointRouteBuilder MapApiDiagnostics()
        {
            app.MapSharedHealthChecks();
            app.MapSharedOpenApi();
            app.MapSharedGrpcReflection();

            return app;
        }
    }
}
