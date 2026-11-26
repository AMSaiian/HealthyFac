using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Testing.Features;
using HealthyFac.Testing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Core.UnitTests.Database;

[Feature(TestFeatures.Persistence)]
[Feature(TestFeatures.Conventions)]
public sealed class AggregateRelationModelTests
{
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    [Fact]
    public void Model_EveryRelation_FollowsAggregateRule()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>().UseNpgsql(UnusedConnectionString).Options;
        using var dbContext = new CoreDbContext(options);

        var wrongRelations = AggregateRelationModelChecks.FindWrongRelations(
            dbContext.GetService<IDesignTimeModel>().Model.GetEntityTypes());

        string.Join(Environment.NewLine, wrongRelations).Should().BeEmpty();
    }
}
