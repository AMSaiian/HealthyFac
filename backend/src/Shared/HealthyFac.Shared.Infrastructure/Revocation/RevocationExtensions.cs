using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Infrastructure.Revocation;

public static class RevocationExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Redis revocation store. Register the Redis connection first with <c>AddSharedRedis</c>.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddTokenRevocationStore()
        {
            services.AddSingleton<ITokenRevocationStore, RedisTokenRevocationStore>();

            return services;
        }
    }
}
