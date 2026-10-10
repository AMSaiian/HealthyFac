using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Web.ErrorHandling;

/// <summary>
/// Provides extension methods for registering and configuring shared exception handling.
/// </summary>
public static class ExceptionHandlingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers shared exception handlers and problem details services.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedExceptionHandling()
        {
            services.AddExceptionHandler<ApplicationExceptionHandler>();
            services.AddExceptionHandler<GenericExceptionHandler>();
            services.AddProblemDetails();

            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Adds the shared exception handler middleware to the application pipeline.
        /// </summary>
        /// <returns>The <see cref="IApplicationBuilder"/> instance for chained configuration.</returns>
        public IApplicationBuilder UseSharedExceptionHandling()
        {
            return app.UseExceptionHandler();
        }
    }
}
