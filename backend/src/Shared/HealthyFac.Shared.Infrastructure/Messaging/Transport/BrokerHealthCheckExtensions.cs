using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

public static class BrokerHealthCheckExtensions
{
    extension(IHealthChecksBuilder builder)
    {
        /// <summary>
        /// Adds the RabbitMQ check on the connection of <see cref="BrokerConnectionProvider"/>. A failure reports
        /// <see cref="HealthStatus.Degraded"/> and not <see cref="HealthStatus.Unhealthy"/>, because the outbox
        /// accepts writes while the broker is down. An unhealthy readiness status would make the gateway stop
        /// every request to the service. The timeout stays below the probe timeout of the gateway.
        /// </summary>
        /// <param name="tags">The tags of the check.</param>
        /// <returns>The updated <see cref="IHealthChecksBuilder"/> instance for method chaining.</returns>
        public IHealthChecksBuilder AddBrokerHealthCheck(IEnumerable<string> tags) =>
            builder.AddRabbitMQ(
                provider => provider.GetRequiredService<BrokerConnectionProvider>().GetConnectionAsync(),
                MessagingConstants.HealthCheck.Name,
                HealthStatus.Degraded,
                tags,
                MessagingConstants.HealthCheck.Timeout);
    }
}
