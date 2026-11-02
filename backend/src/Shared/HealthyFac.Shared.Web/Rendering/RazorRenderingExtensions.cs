using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Web.Rendering;

/// <summary>
/// Provides extension methods for registering Razor component rendering.
/// </summary>
public static class RazorRenderingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="RazorComponentRenderer"/> as a scoped service. It needs a registered
        /// <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/> (see <c>AddLogging</c>).
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddRazorRendering()
        {
            services.AddScoped<RazorComponentRenderer>();

            return services;
        }
    }
}
