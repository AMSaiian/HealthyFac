using Microsoft.Extensions.DependencyInjection;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Application;

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
            var applicationAssembly = typeof(MachineErrors).Assembly;

            services.AddUseCases(applicationAssembly);
            services.AddScoped<EmployeeInviteResolver>();

            return services;
        }
    }
}
