using HealthyFac.Shared.UnitTests.Persistence.Data;
using HealthyFac.Testing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Shared.UnitTests.Persistence;

public sealed class AggregateRelationModelChecksTests
{
    [Fact]
    public void FindWrongRelations_WithPlantedModel_ReportsEachWrongRelation()
    {
        var options = new DbContextOptionsBuilder<AggregateRelationTestModel>()
            .UseNpgsql(AggregateRelationTestModel.UnusedConnectionString)
            .Options;

        using var dbContext = new AggregateRelationTestModel(options);

        var wrongRelations = AggregateRelationModelChecks.FindWrongRelations(
            dbContext.GetService<IDesignTimeModel>().Model.GetEntityTypes());

        wrongRelations.Should()
            .BeEquivalentTo(
                "PlantedCascadedReference.OtherRootId -> PlantedOtherRoot: Cascade, expected Restrict",
                "PlantedCompositeCascadedChild.RootId -> PlantedRoot: soft delete cascade needs a single Guid key",
                "PlantedRestrictedChild.RootId -> PlantedRoot: Restrict, expected Cascade",
                "PlantedRoot.OwnedItems: OwnsMany",
                "PlantedSetNullReference.OtherRootId -> PlantedOtherRoot: SetNull, expected Restrict");
    }
}
