using HealthyFac.Telemetry.IntegrationTests.Database;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricUnits.Data;

namespace HealthyFac.Telemetry.IntegrationTests.MetricUnits;

[Collection(TelemetrySeedingCollection.Name)]
public sealed class MetricUnitSeedTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SeedRequiredDataAsync_OnEmptyCatalog_StoresOneMetricUnitsChangedMessage()
    {
        var count = await _fixture.CountMetricUnitsChangedAsync();

        count.Should().Be(expected: 1);
    }

    [Fact]
    public async Task SeedRequiredDataAsync_WithUnchangedCatalog_StoresNoMessage()
    {
        var before = await _fixture.CountMetricUnitsChangedAsync();

        await _fixture.SeedCatalogAsync();

        var after = await _fixture.CountMetricUnitsChangedAsync();

        after.Should().Be(before);
    }

    [Fact]
    public async Task SeedRequiredDataAsync_WithChangedAbbreviation_StoresOneMoreMessageAndRestoresIt()
    {
        var before = await _fixture.CountMetricUnitsChangedAsync();
        await _fixture.ChangeAbbreviationAsync(
            MetricUnitTestData.CelsiusUnitName, MetricUnitTestData.WrongAbbreviation);

        await _fixture.SeedCatalogAsync();

        var after = await _fixture.CountMetricUnitsChangedAsync();
        var abbreviation = await _fixture.LoadAbbreviationAsync(MetricUnitTestData.CelsiusUnitName);

        after.Should().Be(before + 1);
        abbreviation.Should().Be(MetricUnitTestData.CelsiusAbbreviation);
    }
}
