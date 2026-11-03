using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Web.Authentication;
using HealthyFac.Shared.Web.HealthChecks;
using HealthyFac.Shared.Web.Logging;
using HealthyFac.Shared.Web.Metrics;
using HealthyFac.Shared.Web.Proxy;
using HealthyFac.Shared.Web.Tracing;
using HealthyFac.Spa.Web.Hosting;
using HealthyFac.Spa.Web.IndexDocument;
using HealthyFac.Spa.Web.RuntimeConfig;
using HealthyFac.Spa.Web.StaticAssets;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using Serilog;
using Serilog.Events;

namespace HealthyFac.Spa.Web;

/// <summary>
/// Configures dependency injection, the middleware pipeline, and the endpoints of the SPA host.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated options, the index document, error handling, and observability.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSpaServices(IConfiguration configuration)
        {
            services.AddServiceOptions(configuration);
            services.AddValidatedOptions<SpaOptions, SpaOptionsValidator>(SpaOptions.SectionName, configuration);
            services.AddSingleton(provider => IndexDocument.IndexDocument.Load(
                Path.GetFullPath(
                    provider.GetRequiredService<IOptions<SpaOptions>>().Value.ContentRoot,
                    AppContext.BaseDirectory)));
            services.AddSharedForwardedHeaders(configuration);
            services.AddProblemDetails();
            services.AddAuthorizationBuilder().SetAuthenticatedFallbackPolicy();
            services.AddSpaObservability(configuration);

            return services;
        }

        private IServiceCollection AddSpaObservability(IConfiguration configuration)
        {
            services.AddSharedLogging(configuration);
            services.AddSharedTracing(configuration);
            services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
                options.Filter = context => !StaticAssetRequests.IsStaticAsset(context.Request.Path));
            services.AddSharedMetrics(configuration);
            services.AddSharedHealthChecks(configuration)
                .AddCheck<IndexDocumentHealthCheck>(
                    nameof(IndexDocument.IndexDocument),
                    tags: [HealthCheckConstants.ReadyTag]);

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Configures the HTTP request pipeline. It loads the index document first, so a missing file stops the
        /// startup.
        /// </summary>
        /// <returns>The configured <see cref="WebApplication"/> instance.</returns>
        public WebApplication UseSpaPipeline()
        {
            _ = app.Services.GetRequiredService<IndexDocument.IndexDocument>();

            app.UseSharedHealthChecks();
            app.UseSharedForwardedHeaders();
            app.UseSerilogRequestLogging(options => options.GetLevel = GetRequestLogLevel);
            app.UseExceptionHandler();
            app.UseSpaStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            return app;
        }
    }

    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the index and runtime configuration endpoints.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapSpaEndpoints()
        {
            app.MapRuntimeConfigEndpoints();
            app.MapIndexEndpoints();

            return app;
        }
    }

    /// <summary>
    /// Chooses the log level of a request. A static file request logs at <see cref="LogEventLevel.Debug"/>, so the
    /// many file requests of one page load do not fill the log.
    /// </summary>
    private static LogEventLevel GetRequestLogLevel(
        HttpContext context,
        double elapsedMilliseconds,
        Exception? exception
    )
    {
        if (exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        return StaticAssetRequests.IsStaticAsset(context.Request.Path)
            ? LogEventLevel.Debug
            : LogEventLevel.Information;
    }
}
