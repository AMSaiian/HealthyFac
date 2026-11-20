using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Web.IdEncryption;
using HealthyFac.Shared.Web.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using SignalRRedisOptions = Microsoft.AspNetCore.SignalR.StackExchangeRedis.RedisOptions;

namespace HealthyFac.Shared.Web.Realtime;

/// <summary>
/// Provides extension methods for the real-time hub and its Redis backplane.
/// </summary>
public static class RealtimeExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers SignalR with the JSON protocol only and the Redis backplane on the shared Redis connection. The
        /// hub protocol reads the shared JSON conventions and, when an <see cref="IIdEncryptor"/> is registered, the ID
        /// encryption. The gateway registers none: it serializes no push, because the backplane forwards the bytes
        /// that the pusher wrote. A host that only pushes calls it too and maps no hub.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedRealtime(IConfiguration configuration)
        {
            services.AddSharedRedis(configuration);
            services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();
            services.AddSignalR()
                .AddJsonProtocol()
                .AddStackExchangeRedis();
            services.AddOptions<JsonHubProtocolOptions>()
                .Configure<IServiceProvider>(ConfigureJsonProtocol);
            services.AddOptions<SignalRRedisOptions>()
                .Configure<IServiceProvider>(ConfigureBackplane);

            return services;
        }
    }

    private static void ConfigureJsonProtocol(JsonHubProtocolOptions options, IServiceProvider provider)
    {
        SharedJsonOptionsConfigurator.Apply(options.PayloadSerializerOptions);

        var encryptor = provider.GetService<IIdEncryptor>();
        if (encryptor is not null)
        {
            IdEncryptionJsonOptionsConfigurator.Apply(options.PayloadSerializerOptions, encryptor);
        }
    }

    /// <summary>
    /// Makes the SignalR backplane use the shared <see cref="IConnectionMultiplexer"/> of the container.
    /// </summary>
    /// <param name="options">The backplane options to configure.</param>
    /// <param name="provider">The provider that resolves the shared multiplexer.</param>
    /// <remarks>
    /// The factory returns the shared multiplexer. When the container disposes, the hub lifetime manager disposes that
    /// connection and ends its subscriptions. This is safe, because the hosted services stop before the container
    /// disposes its singletons, and a second <c>Dispose</c> of the multiplexer does nothing.
    /// </remarks>
    private static void ConfigureBackplane(SignalRRedisOptions options, IServiceProvider provider)
    {
        options.ConnectionFactory = _ => Task.FromResult(provider.GetRequiredService<IConnectionMultiplexer>());
    }
}
