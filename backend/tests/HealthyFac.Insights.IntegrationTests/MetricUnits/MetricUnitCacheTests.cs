using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Infrastructure.MetricUnits;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.MetricUnits.Data;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Messaging.MetricUnits;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HealthyFac.Insights.IntegrationTests.MetricUnits;

[Collection(InsightsCollection.Name)]
public sealed class MetricUnitCacheTests(InsightsFixture fixture)
{
    private const string CatalogKey = "insights:metric-units";
    private const int ParallelReaders = 5;
    private const int MinTimeToLiveSeconds = 86400;
    private const int MaxTimeToLiveSeconds = 90000;

    private static readonly TimeSpan SlowFetch = TimeSpan.FromMilliseconds(value: 500);

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task GetAll_WithEmptyCacheAndParallelReaders_CallsTelemetryOnce()
    {
        _fixture.Units.Delay = SlowFetch;

        var results = await Task.WhenAll(
            Enumerable.Range(start: 0, ParallelReaders).Select(_ => ReadCatalogAsync()));

        _fixture.Units.FetchCount.Should().Be(expected: 1);
        results.Should().OnlyContain(units => units.Count == FakeMetricUnitSource.Units.Count);
    }

    [Fact]
    public async Task GetAll_AfterTheFirstRead_ServesFromRedisWithoutCallingTelemetry()
    {
        await ReadCatalogAsync();

        var units = await ReadCatalogAsync();

        _fixture.Units.FetchCount.Should().Be(expected: 1);
        units.Should().BeEquivalentTo(FakeMetricUnitSource.Units);
    }

    [Fact]
    public async Task GetAll_AfterTheFirstRead_StoresTheEntryWithTimeToLiveAndJitter()
    {
        await ReadCatalogAsync();

        var timeToLive = await _fixture.GetSingleton<IConnectionMultiplexer>()
            .GetDatabase()
            .KeyTimeToLiveAsync(CatalogKey);

        timeToLive.Should().NotBeNull();
        timeToLive!.Value.TotalSeconds.Should().BeInRange(MinTimeToLiveSeconds - 1, MaxTimeToLiveSeconds);
    }

    [Fact]
    public async Task MetricUnitsChanged_Always_DeletesTheEntry()
    {
        await ReadCatalogAsync();
        using var scope = _fixture.ScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MetricUnitsChanged>>();

        await handler.HandleAsync(
            new MetricUnitsChanged { ChangedAt = IngestAttemptTestData.Now },
            TestContext.Current.CancellationToken);

        var isKeyPresent = await _fixture.GetSingleton<IConnectionMultiplexer>()
            .GetDatabase()
            .KeyExistsAsync(CatalogKey);

        isKeyPresent.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_AfterTheEntryWasForgotten_CallsTelemetryAgain()
    {
        await ReadCatalogAsync();
        await _fixture.GetSingleton<IMetricUnitCache>().ForgetAsync(TestContext.Current.CancellationToken);

        await ReadCatalogAsync();

        _fixture.Units.FetchCount.Should().Be(expected: 2);
    }

    private Task<IReadOnlyList<MetricUnit>> ReadCatalogAsync() =>
        _fixture.GetSingleton<IMetricUnitCatalog>().GetAllAsync(CancellationToken.None);
}
