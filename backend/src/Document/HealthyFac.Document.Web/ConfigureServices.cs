using System.Threading.RateLimiting;
using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Document.Infrastructure.Files.Storage;
using HealthyFac.Document.Web.Authorization;
using HealthyFac.Document.Web.Files;
using HealthyFac.Document.Web.Files.Rpc;
using HealthyFac.Shared.Infrastructure.Configuration;
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
using HealthyFac.Shared.Web.Rpc;
using HealthyFac.Shared.Web.Serialization;
using HealthyFac.Shared.Web.Tracing;
using HealthyFac.Shared.Web.Versioning;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;

namespace HealthyFac.Document.Web;

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
            services.AddSharedGrpc(environment, configuration);

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
                .AddPermissionPolicies(DocumentPermissions.All);

            return services;
        }

        private IServiceCollection AddApiUploads(IConfiguration configuration)
        {
            services.AddValidatedOptions<UploadLimits, UploadLimitsValidator>(UploadLimits.SectionName, configuration);
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = FileValidationConstants.Size.AttachmentMaxBytes
                    + FileValidationConstants.Size.MultipartOverheadBytes;
                options.MemoryBufferThreshold = UploadLimits.MemoryBufferThresholdBytes;
            });
            services.AddRateLimiter(options => options.RejectionStatusCode = UploadLimits.RejectionStatusCode);
            services.AddOptions<RateLimiterOptions>()
                .Configure<IOptions<UploadLimits>>((options, limits) =>
                    options.AddConcurrencyLimiter(
                        UploadLimits.PolicyName,
                        limiter =>
                        {
                            limiter.PermitLimit = limits.Value.PermitLimit;
                            limiter.QueueLimit = limits.Value.QueueLimit;
                            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        }));

            return services;
        }

        private IServiceCollection AddApiContracts(IConfiguration configuration)
        {
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
            });
            services.AddApiUploads(configuration);
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
                .AddDbContextCheck<DocumentDbContext>(
                    DatabaseConstants.HealthCheckName,
                    tags: [HealthCheckConstants.ReadyTag])
                .AddBrokerHealthCheck([HealthCheckConstants.ReadyTag])
                .AddCheck<BlobStorageHealthCheck>(
                    FileStorageConstants.HealthCheckName,
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
            app.UseSharedGrpcPort();
            app.UseSharedForwardedHeaders();
            app.UseSharedHttps();
            app.UseSerilogRequestLogging();

            app.UseSharedCors();
            app.UseSharedExceptionHandling();
            app.UseAuthentication();
            app.UseActorContext();
            app.UseAuthorization();
            app.UseRateLimiter();

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
            versionedGroup.MapFileEndpoints();
            app.MapGrpcService<DocumentFilesService>();
            app.MapSharedGrpcReflection();

            return app;
        }
    }
}
