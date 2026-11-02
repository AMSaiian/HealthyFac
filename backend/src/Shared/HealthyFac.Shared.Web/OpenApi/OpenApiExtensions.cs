using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace HealthyFac.Shared.Web.OpenApi;

/// <summary>
/// Provides extension methods for registering the OpenAPI document and its dashboard.
/// </summary>
public static class OpenApiExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers OpenAPI document generation. Registration is always safe, because the document and the
        /// dashboard are only exposed by <c>MapSharedOpenApi</c> outside production.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedOpenApi()
        {
            services.AddOpenApi(OpenApiConstants.DocumentName);

            return services;
        }
    }

    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the OpenAPI document and the Scalar dashboard, but only when the host environment is
        /// development. Neither endpoint exists in any other environment.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapSharedOpenApi()
        {
            var environment = app.ServiceProvider.GetRequiredService<IHostEnvironment>();
            if (!environment.IsDevelopment())
            {
                return app;
            }

            app.MapOpenApi(OpenApiConstants.DocumentRoute).AllowAnonymous();
            app.MapScalarApiReference(OpenApiConstants.DashboardRoute).AllowAnonymous();

            return app;
        }
    }
}
