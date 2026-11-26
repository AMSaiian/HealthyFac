using System.Security.Cryptography;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Ingest.Data;

namespace HealthyFac.Seeder.IntegrationTests.Ingest;

[Collection(SeederCollection.Name)]
public sealed class IngestSeedTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheRealIngestSets_StoresTheSixFilesWithTheirBlobsAndTheCompletedState()
    {
        await _fixture.RunIngestSeedAsync(TestContext.Current.CancellationToken);

        var files = await _fixture.LoadFileRefsAsync();
        files.Should().HaveCount(IngestTestData.FileCount);
        files.Should().OnlyContain(file => file.IngestState == FileIngestState.Completed);

        foreach (var file in files)
        {
            var committed = await File.ReadAllBytesAsync(
                Path.Combine(
                    AppContext.BaseDirectory,
                    IngestTestData.DocumentPaths.Single(path => path.Contains(file.FileName))),
                TestContext.Current.CancellationToken);

            var blob = await _fixture.ReadBlobAsync(file.Id);
            SHA256.HashData(blob).Should().Equal(SHA256.HashData(committed));
            file.Sha256.Should().Equal(SHA256.HashData(committed));
        }
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheRealIngestSets_StoresTheIngestResultsInInsightsAndTheDraftsInCoreAndTelemetry()
    {
        await _fixture.RunIngestSeedAsync(TestContext.Current.CancellationToken);

        var insights = await _fixture.CountInsightsAsync();
        var machineDraftCount = await _fixture.CountPendingMachineDraftsAsync();
        var telemetry = await _fixture.CountPendingDefinitionDraftsAsync();
        insights.CompletedAttempts.Should().Be(IngestTestData.FileCount);
        insights.Documents.Should().Be(IngestTestData.FileCount);
        insights.Chunks.Should().Be(IngestTestData.ChunkCount);
        insights.Runs.Should().Be(IngestTestData.RunCount);
        insights.ToolCalls.Should().Be(IngestTestData.ToolCallCount);
        insights.VectorLengths.Should().Equal(IngestTestData.VectorLength);
        machineDraftCount.Should().Be(IngestTestData.FileCount);
        telemetry.Metrics.Should().Be(IngestTestData.MetricDraftCount);
        telemetry.Statuses.Should().Be(IngestTestData.StatusDraftCount);
        telemetry.Phases.Should().Be(IngestTestData.PhaseDraftCount);
        telemetry.Entries.Should().Be(IngestTestData.FileCount);
    }
}
