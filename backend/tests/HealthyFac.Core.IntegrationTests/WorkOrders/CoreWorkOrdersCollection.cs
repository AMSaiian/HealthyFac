using HealthyFac.Core.IntegrationTests.Database.Data;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[CollectionDefinition(Name)]
public sealed class CoreWorkOrdersCollection : ICollectionFixture<CoreDatabaseFixture>
{
    public const string Name = "CoreWorkOrders";
}
