using HealthyFac.Core.IntegrationTests.Database.Data;

namespace HealthyFac.Core.IntegrationTests.Shifts;

[CollectionDefinition(Name)]
public sealed class CoreShiftsCollection : ICollectionFixture<CoreDatabaseFixture>
{
    public const string Name = "CoreShifts";
}
