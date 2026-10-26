using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthyFac.Shared.Infrastructure.Time;

/// <summary>
/// Provides extension methods for registering the clock of a service.
/// </summary>
public static class TimeExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="TimeProvider.System"/> as the clock of the service. Components read the time
        /// through <see cref="TimeProvider"/>, so a test replaces it with a fake clock.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedTime()
        {
            services.TryAddSingleton(TimeProvider.System);

            return services;
        }
    }
}
