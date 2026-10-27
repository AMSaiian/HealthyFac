using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rebus.Config;
using Rebus.Retry.FailFast;
using Rebus.Retry.Simple;
using Rebus.Serialization.Json;

namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

public static class TransportExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Rebus with the RabbitMQ transport, the input queue of the service, and System.Text.Json
        /// serialization. Rebus keeps its default <c>error</c> queue, makes
        /// <see cref="MessagingConstants.MaxDeliveryAttempts"/> delivery attempts, and moves an
        /// <see cref="InvalidIntegrationMessageException"/> to the queue at the first attempt. The envelope handler stores
        /// each delivery in the inbox, and the dispatcher calls the use case of an inbox row. The subscription service
        /// binds the queue to every message type that
        /// <see cref="AddIntegrationMessage{TMessage}"/> registers. The host starts the bus. The
        /// method also registers the <see cref="BrokerConnectionProvider"/> of the readiness check.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the <c>ConnectionStrings:RabbitMq</c> value is missing or blank.
        /// </exception>
        public IServiceCollection AddMessagingTransport(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(MessagingConstants.RabbitMqConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw MessagingErrors.MissingBrokerConnectionString();
            }

            services.AddValidatedOptions<MessagingOptions, MessagingOptionsValidator>(
                MessagingOptions.SectionName,
                configuration);
            services.AddIntegrationMessageRegistry();
            services.AddScoped<IntegrationMessageDispatcher>();
            services.AddSingleton(_ => new BrokerConnectionProvider(new Uri(connectionString)));

            services.AddRebus((configurer, provider) => configurer
                .Transport(transport => transport
                    .UseRabbitMq(
                        connectionString,
                        provider.GetRequiredService<IOptions<MessagingOptions>>().Value.QueueName)
                    .SetPublisherConfirms(true))
                .Serialization(serialization => serialization.UseSystemTextJson())
                .Options(options =>
                {
                    options.RetryStrategy(maxDeliveryAttempts: MessagingConstants.MaxDeliveryAttempts);
                    options.FailFastOn<InvalidIntegrationMessageException>();
                }));
            services.AddRebusHandler<IntegrationEnvelopeHandler>();
            services.AddHostedService<SubscriptionHostedService>();

            return services;
        }

        /// <summary>
        /// Adds the message type to the registry, so the transport subscribes to its topic and the inbox
        /// processor dispatches its rows to the use case of the type. The method registers no handler:
        /// <c>AddUseCases</c> already did. Call <c>AddUseCases</c> (the service <c>AddApplicationServices</c>)
        /// before this method, because the method checks the registered use cases.
        /// </summary>
        /// <typeparam name="TMessage">The message contract. It is also the request of its use case.</typeparam>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no use case handles <typeparamref name="TMessage"/> yet (see
        /// <see cref="MessagingErrors.MessageHandlerMissing"/>), or when the message type is already registered.
        /// </exception>
        public IServiceCollection AddIntegrationMessage<TMessage>()
            where TMessage : IIntegrationMessage, IRequest
        {
            var isHandlerRegistered = services.Any(descriptor =>
                descriptor.ServiceType == typeof(IRequestHandler<TMessage, Unit>));
            if (!isHandlerRegistered)
            {
                throw MessagingErrors.MessageHandlerMissing(TMessage.MessageType);
            }

            services.AddIntegrationMessageRegistry().Register<TMessage>();

            return services;
        }

        private IntegrationMessageRegistry AddIntegrationMessageRegistry()
        {
            var registry = services
                .Select(descriptor => descriptor.ImplementationInstance)
                .OfType<IntegrationMessageRegistry>()
                .FirstOrDefault();
            if (registry is not null)
            {
                return registry;
            }

            registry = new IntegrationMessageRegistry();
            services.AddSingleton(registry);

            return registry;
        }
    }
}
