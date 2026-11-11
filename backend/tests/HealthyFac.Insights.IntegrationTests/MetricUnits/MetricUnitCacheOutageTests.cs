using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Infrastructure.MetricUnits;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.MetricUnits.Data;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.IntegrationTests.MetricUnits;

[Collection(MetricUnitOutageCollection.Name)]
public sealed class MetricUnitCacheOutageTests(InsightsFixture fixture)
{
    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task GetAll_WhenRedisIsStopped_ReturnsTheUnitsAndLogsOneWarning()
    {
        await _fixture.StopRedisAsync();

        var units = await _fixture.GetSingleton<IMetricUnitCatalog>().GetAllAsync(CancellationToken.None);

        var warnings = _fixture.Logs.Entries
            .Where(entry => entry.Level == LogLevel.Warning
                && entry.Category == typeof(RedisMetricUnitCatalog).FullName)
            .ToList();

        units.Should().BeEquivalentTo(FakeMetricUnitSource.Units);
        warnings.Should().ContainSingle();
    }
}
