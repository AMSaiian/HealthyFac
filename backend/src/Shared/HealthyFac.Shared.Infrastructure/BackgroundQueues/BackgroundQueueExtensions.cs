using HealthyFac.Shared.Application.BackgroundQueues;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Infrastructure.BackgroundQueues;

/// <summary>
/// Provides extension methods for registering a background queue.
/// </summary>
public static class BackgroundQueueExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the queue and the sender of one item type, and a start check. The host start fails unless
        /// exactly 1 <see cref="IBackgroundItemHandler{TItem}"/> is registered for the item type. A second call for
        /// the same item type registers nothing new.
        /// </summary>
        /// <typeparam name="TItem">The type of the queued item.</typeparam>
        /// <param name="capacity">The capacity of the queue.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddBackgroundQueue<TItem>(BackgroundQueueCapacity capacity)
        {
            var isRegistered = services.Any(descriptor => descriptor.ServiceType == typeof(BackgroundQueue<TItem>));
            if (isRegistered)
            {
                return services;
            }

            services.AddSingleton(provider => new BackgroundQueue<TItem>(
                capacity,
                provider.GetService<IBackgroundItemDropObserver<TItem>>()));
            services.AddSingleton<IBackgroundQueue<TItem>>(provider =>
                provider.GetRequiredService<BackgroundQueue<TItem>>());
            services.AddHostedService<BackgroundQueueStartCheck<TItem>>();
            services.AddHostedService<BackgroundQueueSender<TItem>>();

            return services;
        }
    }
}
