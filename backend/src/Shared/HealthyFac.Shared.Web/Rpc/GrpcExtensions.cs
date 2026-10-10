using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.Web.Rpc;

/// <summary>
/// Provides extension methods for hosting gRPC services and for resilient gRPC client channels.
/// </summary>
public static class GrpcExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the gRPC server. Detailed error messages and the reflection service are enabled only
        /// outside production, so a release build never advertises its service descriptors.
        /// </summary>
        /// <param name="environment">The host environment deciding whether to expose reflection.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedGrpc(IHostEnvironment environment)
        {
            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = environment.IsDevelopment();
                options.MaxReceiveMessageSize = GrpcConstants.MaxReceiveMessageSizeBytes;
            });

            if (environment.IsDevelopment())
            {
                services.AddGrpcReflection();
            }

            return services;
        }
    }

    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the gRPC reflection service when the host environment is development.
        /// Call this alongside the <c>MapGrpcService</c> calls of each service.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapSharedGrpcReflection()
        {
            var environment = app.ServiceProvider.GetRequiredService<IHostEnvironment>();
            if (environment.IsDevelopment())
            {
                app.MapGrpcReflectionService();
            }

            return app;
        }
    }

    /// <summary>
    /// Builds the retry service config applied to resilient gRPC client channels.
    /// Retries only status codes that are safe to repeat, so a failed write is never duplicated.
    /// </summary>
    /// <returns>The <see cref="ServiceConfig"/> carrying the shared retry policy.</returns>
    public static ServiceConfig CreateResilientServiceConfig()
        => new()
        {
            MethodConfigs =
            {
                new MethodConfig
                {
                    Names = { MethodName.Default },
                    RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = GrpcConstants.Retry.MaxAttempts,
                        InitialBackoff = TimeSpan.FromSeconds(GrpcConstants.Retry.InitialBackoffSeconds),
                        MaxBackoff = TimeSpan.FromSeconds(GrpcConstants.Retry.MaxBackoffSeconds),
                        BackoffMultiplier = GrpcConstants.Retry.BackoffMultiplier,
                        RetryableStatusCodes =
                        {
                            StatusCode.Unavailable,
                            StatusCode.ResourceExhausted,
                            StatusCode.Aborted,
                        },
                    },
                },
            },
        };
}
