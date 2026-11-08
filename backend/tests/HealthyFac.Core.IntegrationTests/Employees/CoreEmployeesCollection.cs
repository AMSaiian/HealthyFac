using HealthyFac.Core.IntegrationTests.Database.Data;

namespace HealthyFac.Core.IntegrationTests.Employees;

[CollectionDefinition(Name)]
public sealed class CoreEmployeesCollection : ICollectionFixture<CoreDatabaseFixture>
{
    public const string Name = "CoreEmployees";
}
