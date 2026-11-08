using HealthyFac.Core.IntegrationTests.Database.Data;

namespace HealthyFac.Core.IntegrationTests.Database;

[CollectionDefinition(Name)]
public sealed class CoreSeedingCollection : ICollectionFixture<CoreDatabaseFixture>
{
    public const string Name = "CoreSeeding";
}
