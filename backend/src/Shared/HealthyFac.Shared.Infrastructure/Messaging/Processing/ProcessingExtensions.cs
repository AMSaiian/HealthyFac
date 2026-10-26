using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Infrastructure.Messaging.Processing;

public static class ProcessingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the background processors of the outbox and the inbox, and the scoped classes that do the
        /// work of one message. Call it after <c>AddMessagingTransport</c>, because the publisher uses the Rebus
        /// bus and the processors read the messaging options.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddMessageProcessors()
        {
            services.AddActorContext();
            services.AddScoped<OutboxMessagePublisher>();
            services.AddScoped<InboxMessageRunner>();
            services.AddHostedService<OutboxProcessor>();
            services.AddHostedService<InboxProcessor>();

            return services;
        }
    }
}
