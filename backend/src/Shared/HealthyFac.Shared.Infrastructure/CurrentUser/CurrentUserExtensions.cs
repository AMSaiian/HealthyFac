using HealthyFac.Shared.Application.CurrentUser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthyFac.Shared.Infrastructure.CurrentUser;

public static class CurrentUserExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the scoped actor holder and the <see cref="ICurrentUserProvider"/> that reads it. Every host
        /// shape can call it, because a second call changes nothing.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddActorContext()
        {
            services.TryAddScoped<ActorContextHolder>();
            services.TryAddScoped<ICurrentUserProvider, ActorCurrentUserProvider>();

            return services;
        }
    }
}
