using HealthyFac.Shared.IntegrationTests.Grids.Data;

namespace HealthyFac.Shared.IntegrationTests.Grids;

[CollectionDefinition(Name)]
public sealed class GridDatabaseCollection : ICollectionFixture<GridDatabaseFixture>
{
    public const string Name = "GridDatabase";
}
