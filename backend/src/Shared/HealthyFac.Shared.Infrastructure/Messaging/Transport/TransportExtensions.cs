using HealthyFac.Shared.Application.Messaging;
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
        /// each delivery in the inbox, and the dispatcher calls the handler of an inbox row. The subscription service
        /// binds the queue to every message type that
        /// <see cref="AddIntegrationMessageHandler{TMessage,THandler}"/> registers. The host starts the bus. The
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
        /// processor dispatches its rows, and registers the scoped handler of the type. A service has exactly one
        /// handler for each message type.
        /// </summary>
        /// <typeparam name="TMessage">The message contract.</typeparam>
        /// <typeparam name="THandler">The handler of the message.</typeparam>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <typeparamref name="TMessage"/> is already registered. The check runs before the handler is
        /// added, so a rejected registration leaves no second handler in the container.
        /// </exception>
        public IServiceCollection AddIntegrationMessageHandler<TMessage, THandler>()
            where TMessage : IIntegrationMessage
            where THandler : class, IIntegrationMessageHandler<TMessage>
        {
            services.AddIntegrationMessageRegistry().Register<TMessage>();
            services.AddScoped<IIntegrationMessageHandler<TMessage>, THandler>();

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
