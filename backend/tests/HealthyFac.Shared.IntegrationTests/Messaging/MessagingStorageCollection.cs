using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

[CollectionDefinition(Name)]
public sealed class MessagingStorageCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "MessagingStorage";
}
