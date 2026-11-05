using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Auth.Application;

/// <summary>
/// Registers application layer services into the dependency injection container.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the use cases (handlers, decorators, and validators) and the services that they share.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddApplicationServices()
        {
            services.AddUseCases(AuthApplicationAssembly.Instance);

            return services;
        }
    }
}
