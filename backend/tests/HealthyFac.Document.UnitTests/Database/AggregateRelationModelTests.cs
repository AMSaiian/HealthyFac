using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Testing.Features;
using HealthyFac.Testing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Document.UnitTests.Database;

[Feature(TestFeatures.Persistence)]
public sealed class AggregateRelationModelTests
{
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    [Fact]
    public void Model_EveryRelation_FollowsAggregateRule()
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>().UseNpgsql(UnusedConnectionString).Options;
        using var dbContext = new DocumentDbContext(options);

        var wrongRelations = AggregateRelationModelChecks.FindWrongRelations(
            dbContext.GetService<IDesignTimeModel>().Model.GetEntityTypes());

        string.Join(Environment.NewLine, wrongRelations).Should().BeEmpty();
    }
}
