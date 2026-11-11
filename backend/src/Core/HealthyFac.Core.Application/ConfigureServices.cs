using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddUseCases(CoreApplicationAssembly.Instance);
            services.AddScoped<EmployeeInviteResolver>();
            services.AddScoped<OwnEmployeeResolver>();
            services.AddScoped<WorkOrderSearchQuery>();
            services.AddScoped<DayScheduleReader>();
            services.AddScoped<ShiftChangeGuard>();
            services.AddScoped<DayScheduleBuilder>();
            services.AddScoped<ScheduleChecks>();

            return services;
        }
    }
}
