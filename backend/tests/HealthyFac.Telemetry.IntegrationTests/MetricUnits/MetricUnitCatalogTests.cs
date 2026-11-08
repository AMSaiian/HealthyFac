using HealthyFac.Telemetry.IntegrationTests.Database;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricUnits.Data;

namespace HealthyFac.Telemetry.IntegrationTests.MetricUnits;

[Collection(TelemetrySeedingCollection.Name)]
public sealed class MetricUnitCatalogTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SeedRequiredDataAsync_OnEmptyDatabase_CreatesCatalog()
    {
        var pairs = await _fixture.LoadCatalogPairsAsync();

        pairs.Should().BeEquivalentTo(MetricUnitTestData.ExpectedPairs);
    }

    [Fact]
    public async Task SeedRequiredDataAsync_RunTwice_ChangesNothing()
    {
        var kindsBefore = await _fixture.CountQuantityKindsAsync();
        var pairsBefore = await _fixture.LoadCatalogPairsAsync();

        await _fixture.SeedCatalogAsync();

        var kindsAfter = await _fixture.CountQuantityKindsAsync();
        var pairsAfter = await _fixture.LoadCatalogPairsAsync();

        kindsAfter.Should().Be(kindsBefore);
        pairsAfter.Should().HaveCount(pairsBefore.Count).And.BeEquivalentTo(pairsBefore);
    }

    [Fact]
    public async Task SeedRequiredDataAsync_RunInParallel_Succeeds()
    {
        var act = () => Task.WhenAll(
            Enumerable.Range(start: 0, MetricUnitTestData.ParallelRunCount).Select(_ => _fixture.SeedCatalogAsync()));

        await act.Should().NotThrowAsync();
        var pairs = await _fixture.LoadCatalogPairsAsync();

        pairs.Should().HaveCount(MetricUnitTestData.ExpectedPairs.Count);
    }

    [Fact]
    public async Task ListMetricUnits_AfterSeed_ExcludesDiscreteKind()
    {
        var response = await _fixture.ListMetricUnitsAsync();

        response.Kinds.Select(kind => kind.Name).Should().NotContain(unexpected: "Discrete");
    }

    [Fact]
    public async Task ListMetricUnits_AfterSeed_ReturnsKindsWithUnitsSorted()
    {
        var response = await _fixture.ListMetricUnitsAsync();

        var temperature = response.Kinds.Single(kind => kind.Name == "Temperature");

        response.Kinds.Select(kind => kind.Name).Should().BeInAscendingOrder(StringComparer.Ordinal);
        temperature.Units.Select(unit => unit.Name).Should().Equal("DegreeCelsius", "DegreeFahrenheit", "Kelvin");
        temperature.Units[index: 0].Abbreviation.Should().Be(MetricUnitTestData.CelsiusAbbreviation);
    }
}
