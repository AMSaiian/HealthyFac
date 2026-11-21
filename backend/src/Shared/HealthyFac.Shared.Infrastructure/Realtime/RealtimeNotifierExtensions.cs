using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Infrastructure.BackgroundQueues;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Infrastructure.Realtime;

/// <summary>
/// Provides extension methods for registering the real-time notifier.
/// </summary>
public static class RealtimeNotifierExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the scoped <see cref="IRealtimeNotifier"/>, the inbox observer that pushes its events after a
        /// commit, and the unbounded background queue of <see cref="RealtimePush"/>. The service Web project
        /// registers the handler of the queue (<c>AddSharedRealtime</c>), because only it references the hub.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddRealtimeNotifier()
        {
            services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();
            services.AddScoped<IInboxUnitObserver, RealtimeInboxObserver>();
            services.AddBackgroundQueue<RealtimePush>(BackgroundQueueCapacity.Unbounded);

            return services;
        }
    }
}
