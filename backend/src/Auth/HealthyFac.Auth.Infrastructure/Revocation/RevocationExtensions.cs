using HealthyFac.Auth.Application.Identities;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.Revocation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Auth.Infrastructure.Revocation;

/// <summary>
/// Provides extension methods for registering session revocation.
/// </summary>
public static class RevocationExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Redis connection, the shared revocation list, the validated revocation options, and
        /// <see cref="ISessionRevoker"/>. Register <c>AddSharedTime</c> first, because the Redis breaker needs it.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSessionRevocation(IConfiguration configuration)
        {
            services.AddSharedRedis(configuration);
            services.AddTokenRevocationStore();
            services.AddValidatedOptions<RevocationOptions, RevocationOptionsValidator>(
                RevocationOptions.SectionName,
                configuration);
            services.AddSingleton<ISessionRevoker, RedisSessionRevoker>();

            return services;
        }
    }
}
