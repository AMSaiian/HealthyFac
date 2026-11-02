using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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

        /// <summary>
        /// Registers the validated <see cref="ServiceOptions"/> that name the running service. Every host calls it
        /// once, because the processors, the start-up, and the telemetry read the name from the options.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance containing
        /// settings.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> instance for chained configuration.</returns>
        public OptionsBuilder<ServiceOptions> AddServiceOptions(IConfiguration configuration) =>
            services.AddValidatedOptions<ServiceOptions, ServiceOptionsValidator>(
                ServiceOptions.SectionName,
                configuration);
    }
}
