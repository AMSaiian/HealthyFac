using HealthyFac.Core.IntegrationTests.Database.Data;

namespace HealthyFac.Core.IntegrationTests.Machines;

[CollectionDefinition(Name)]
public sealed class CoreMachinesCollection : ICollectionFixture<CoreDatabaseFixture>
{
    public const string Name = "CoreMachines";
}
