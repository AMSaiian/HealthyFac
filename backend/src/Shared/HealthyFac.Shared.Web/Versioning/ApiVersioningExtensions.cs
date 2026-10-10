using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Web.Versioning;

/// <summary>
/// Provides extension methods for registering and configuring API versioning.
/// </summary>
public static class ApiVersioningExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers shared API versioning services with custom option adjustments.
        /// </summary>
        /// <param name="configureOptions">Action to customize <see cref="ApiVersioningOptions"/>.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedApiVersioning(Action<ApiVersioningOptions>? configureOptions = null)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(ApiVersioningConstants.DefaultMajorVersion);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();

                configureOptions?.Invoke(options);
            });

            return services;
        }
    }

    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Creates a route group prefixed with the API version URL segment.
        /// </summary>
        /// <returns>A configured <see cref="RouteGroupBuilder"/> with API versioning enabled.</returns>
        public RouteGroupBuilder MapVersionedGroup()
        {
            var versionedApi = app.NewVersionedApi();

            return versionedApi.MapGroup(ApiVersioningConstants.RoutePrefix)
                .HasApiVersion(new ApiVersion(ApiVersioningConstants.DefaultMajorVersion));
        }

        /// <summary>
        /// Creates a route group prefixed with the API version URL segment for a specific major version.
        /// </summary>
        /// <param name="majorVersion">The major version number.</param>
        /// <returns>A configured <see cref="RouteGroupBuilder"/> with API versioning enabled.</returns>
        public RouteGroupBuilder MapVersionedGroup(int majorVersion)
        {
            var versionedApi = app.NewVersionedApi();

            return versionedApi.MapGroup(ApiVersioningConstants.RoutePrefix)
                .HasApiVersion(new ApiVersion(majorVersion));
        }
    }
}
