using HealthyFac.Shared.UnitTests.Conventions.Data;
using HealthyFac.Testing.Conventions;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.Conventions;

[Feature(TestFeatures.Conventions)]
public sealed class BackgroundItemConventionTests
{
    [Fact]
    public void FindBackgroundItemViolations_WithPlantedTypes_ReportsOnlyTheBrokenRules()
    {
        var violations = ConventionChecks.FindBackgroundItemViolations(
        [
            typeof(PlantedInterfaceItemHandler),
            typeof(PlantedFirstSharedItemHandler),
            typeof(PlantedSecondSharedItemHandler),
            typeof(PlantedCleanItemHandler),
        ]);

        violations.Should()
            .Equal(
                $"{typeof(PlantedInterfaceItem).FullName}.Context: type DbContext",
                $"{typeof(PlantedInterfaceItem).FullName}.Names: type IReadOnlyList`1",
                $"{typeof(PlantedInterfaceItem).FullName}.Resource: type IDisposable",
                $"{typeof(PlantedInterfaceItem).FullName}.Services: type IServiceProvider",
                $"{typeof(PlantedSharedItem).FullName}: has 2 handlers");
    }

    [Fact]
    public void FindBackgroundItemViolations_WithTheSourceAssemblies_ReportsNothing()
    {
        var types = BackgroundItemConventionTestData.SourceAssemblyLabels.SelectMany(label =>
            label.Assembly.GetTypes());

        var violations = ConventionChecks.FindBackgroundItemViolations(types);

        violations.Should().BeEmpty();
    }
}
