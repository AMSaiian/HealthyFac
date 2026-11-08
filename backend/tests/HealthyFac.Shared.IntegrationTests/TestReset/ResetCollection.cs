using HealthyFac.Shared.IntegrationTests.TestReset.Data;

namespace HealthyFac.Shared.IntegrationTests.TestReset;

[CollectionDefinition(Name)]
public sealed class ResetCollection : ICollectionFixture<ResetFixture>
{
    public const string Name = "TestReset";
}
