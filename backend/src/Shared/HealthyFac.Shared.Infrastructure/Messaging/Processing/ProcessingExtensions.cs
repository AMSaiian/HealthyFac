using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthyFac.Shared.Infrastructure.Messaging.Processing;

public static class ProcessingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the background processors of the outbox and the inbox, and the scoped classes that do the
        /// work of one message. Call it after <c>AddMessagingTransport</c>, because the publisher uses the Rebus
        /// bus and the processors read the messaging options. It also registers the messaging metrics and the
        /// backlog sampler that feeds their gauges.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddMessageProcessors()
        {
            services.AddActorContext();
            services.AddMessagingMetrics();
            services.AddScoped<OutboxMessagePublisher>();
            services.AddScoped<InboxMessageRunner>();
            services.AddHostedService<OutboxProcessor>();
            services.AddHostedService<InboxProcessor>();
            services.AddHostedService<MessageBacklogSampler>();

            return services;
        }

        /// <summary>
        /// Registers the meter factory and the one <see cref="MessagingMetrics"/> instance that the processors and the
        /// backlog sampler share.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        internal IServiceCollection AddMessagingMetrics()
        {
            services.AddMetrics();
            services.TryAddSingleton<MessagingMetrics>();

            return services;
        }
    }
}
