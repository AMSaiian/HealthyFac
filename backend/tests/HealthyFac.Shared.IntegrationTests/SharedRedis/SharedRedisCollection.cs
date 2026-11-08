using HealthyFac.Shared.IntegrationTests.SharedRedis.Data;

namespace HealthyFac.Shared.IntegrationTests.SharedRedis;

[CollectionDefinition(Name)]
public sealed class SharedRedisCollection : ICollectionFixture<SharedRedisFixture>
{
    public const string Name = "SharedRedis";
}
