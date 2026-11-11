using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Web.Rpc;

/// <summary>
/// Provides extension methods for hosting gRPC services and for resilient gRPC client channels.
/// </summary>
public static class GrpcExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the gRPC server, the exception interceptor, the port middleware, and the validated gRPC port.
        /// The endpoint on the gRPC port speaks HTTP/2 only (h2c). Detailed error messages and the reflection
        /// service are enabled only outside production, so a release build never advertises its service descriptors.
        /// </summary>
        /// <remarks>
        /// The method opens no port. The host must list the gRPC port in <c>ASPNETCORE_HTTP_PORTS</c> or in the launch
        /// URLs, as it does for the health port. Kestrel then keeps the scheme and the host of each address. A listener
        /// in code would replace those addresses. Without the port in the address list, no gRPC call reaches the
        /// service.
        /// </remarks>
        /// <param name="environment">The host environment deciding whether to expose reflection.</param>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedGrpc(IHostEnvironment environment, IConfiguration configuration)
        {
            services.AddValidatedOptions<GrpcEndpointOptions, GrpcEndpointOptionsValidator>(
                GrpcEndpointOptions.SectionName,
                configuration);
            services.AddSingleton<GrpcPortMiddleware>();
            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = environment.IsDevelopment();
                options.MaxReceiveMessageSize = GrpcConstants.MaxReceiveMessageSizeBytes;
                options.Interceptors.Add<GrpcExceptionInterceptor>();
            });
            services.AddOptions<KestrelServerOptions>()
                .Configure<IOptions<GrpcEndpointOptions>>((kestrel, grpcOptions) =>
                    kestrel.ConfigureEndpointDefaults(listen =>
                    {
                        if (listen.IPEndPoint?.Port == grpcOptions.Value.Port)
                        {
                            listen.Protocols = HttpProtocols.Http2;
                        }
                    }));

            if (environment.IsDevelopment())
            {
                services.AddGrpcReflection();
            }

            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Keeps gRPC calls on the gRPC port and every other request off it (see <see cref="GrpcPortMiddleware"/>).
        /// </summary>
        /// <remarks>Call it right after <c>UseSharedHealthChecks()</c>, before routing.</remarks>
        /// <returns>The <see cref="IApplicationBuilder"/> instance for chained configuration.</returns>
        public IApplicationBuilder UseSharedGrpcPort() => app.UseMiddleware<GrpcPortMiddleware>();
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
                app.MapGrpcReflectionService().AllowAnonymous();
            }

            return app;
        }
    }
}
