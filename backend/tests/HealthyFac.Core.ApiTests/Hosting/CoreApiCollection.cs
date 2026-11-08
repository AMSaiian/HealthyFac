namespace HealthyFac.Core.ApiTests.Hosting;

[CollectionDefinition(Name)]
public sealed class CoreApiCollection : ICollectionFixture<CoreApiFixture>
{
    public const string Name = "CoreApi";
}
