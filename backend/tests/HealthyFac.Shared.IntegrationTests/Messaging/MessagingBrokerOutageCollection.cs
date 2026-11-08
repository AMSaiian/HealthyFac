using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

[CollectionDefinition(Name)]
public sealed class MessagingBrokerOutageCollection : ICollectionFixture<TransportCollectionFixture>
{
    public const string Name = "MessagingBrokerOutage";
}
