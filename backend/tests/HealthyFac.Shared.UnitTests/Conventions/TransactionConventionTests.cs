using HealthyFac.Shared.Infrastructure;
using HealthyFac.Shared.UnitTests.Conventions.Data;
using HealthyFac.Testing.Conventions;

namespace HealthyFac.Shared.UnitTests.Conventions;

public sealed class TransactionConventionTests
{
    [Fact]
    public void FindTransactionStarts_WithPlantedTypes_ReportsEachStart()
    {
        var violations = ConventionChecks.FindTransactionStarts(
        [
            typeof(PlantedSyncTransactionStart),
            typeof(PlantedAsyncTransactionStart),
            typeof(PlantedLambdaTransactionStart),
            typeof(PlantedTransactionReader),
        ]);

        violations.Should()
            .Equal(
                $"{typeof(PlantedAsyncTransactionStart).FullName}: calls DatabaseFacade.BeginTransactionAsync",
                $"{typeof(PlantedLambdaTransactionStart).FullName}: calls RelationalDatabaseFacadeExtensions"
                + ".BeginTransactionAsync",
                $"{typeof(PlantedSyncTransactionStart).FullName}: calls DatabaseFacade.BeginTransaction");
    }

    [Fact]
    public void FindTransactionStarts_InSharedInfrastructure_AllowsTransactionRunner()
    {
        var violations = ConventionChecks.FindTransactionStarts(SharedInfrastructureAssembly.Instance);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void FindContextsWithoutSaveTransaction_WithPlantedTypes_ReportsEachMissingOverride()
    {
        var violations = ConventionChecks.FindContextsWithoutSaveTransaction(
        [
            typeof(PlantedContextWithoutSaveOverride),
            typeof(PlantedContextWithoutSyncSaveOverride),
            typeof(PlantedContextWithSaveOverride),
            typeof(PlantedTransactionReader),
        ]);

        violations.Should()
            .Equal(
                $"{typeof(PlantedContextWithoutSaveOverride).FullName}: SaveChanges(bool) "
                + "does not call TransactionRunner.Save",
                $"{typeof(PlantedContextWithoutSaveOverride).FullName}: SaveChangesAsync(bool, CancellationToken) "
                + "does not call TransactionRunner.SaveAsync",
                $"{typeof(PlantedContextWithoutSyncSaveOverride).FullName}: SaveChanges(bool) "
                + "does not call TransactionRunner.Save");
    }
}
