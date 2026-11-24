namespace HealthyFac.Seeder.IntegrationTests.Hosting;

[CollectionDefinition(Name)]
public sealed class SeederCollection : ICollectionFixture<SeederFixture>
{
    public const string Name = "Seeder";
}
