using HealthyFac.Core.IntegrationTests.Concurrency.Data;

namespace HealthyFac.Core.IntegrationTests.Concurrency;

[CollectionDefinition(Name)]
public sealed class CoreSaveGateCollection : ICollectionFixture<GateDatabaseFixture>
{
    public const string Name = "CoreSaveGate";
}
