namespace HealthyFac.Document.ApiTests.Hosting;

[CollectionDefinition(Name)]
public sealed class DocumentApiCollection : ICollectionFixture<DocumentApiFixture>
{
    public const string Name = "DocumentApi";
}
