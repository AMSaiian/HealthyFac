using HealthyFac.Document.IntegrationTests.Database.Data;

namespace HealthyFac.Document.IntegrationTests.Database;

[CollectionDefinition(Name)]
public sealed class DocumentDatabaseCollection : ICollectionFixture<DocumentDatabaseFixture>
{
    public const string Name = "DocumentDatabase";
}
