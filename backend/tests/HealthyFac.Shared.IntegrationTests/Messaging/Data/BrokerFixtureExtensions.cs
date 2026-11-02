using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class BrokerFixtureExtensions
{
    extension(TransportFixture fixture)
    {
        public BrokerConnectionProvider CreateBrokerConnectionProvider() =>
            new(new Uri(fixture.BrokerConnectionString));

        public ServiceProvider BuildBrokerHealthCheckServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(fixture.CreateBrokerConnectionProvider());
            services.AddHealthChecks().AddBrokerHealthCheck([]);

            return services.BuildServiceProvider();
        }
    }
}
