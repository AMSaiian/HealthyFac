using Azure.Storage.Blobs;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.Ingest.Data;

public static class IngestFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        public async Task<List<FileRef>> LoadFileRefsAsync()
        {
            using var scope = fixture.Hosts.Document.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<DocumentDbContext>()
                .FileRefs
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<byte[]> ReadBlobAsync(Guid fileId)
        {
            using var scope = fixture.Hosts.Document.CreateScope(SeederTestData.InitializationActor);

            var blob = scope.ServiceProvider.GetRequiredService<BlobServiceClient>()
                .GetBlobContainerClient(IngestTestData.BlobContainer)
                .GetBlobClient(fileId.ToString());

            var content = await blob.DownloadContentAsync();

            return content.Value.Content.ToArray();
        }

        public async Task<InsightsCounts> CountInsightsAsync()
        {
            using var scope = fixture.Hosts.Insights.CreateScope(SeederTestData.InitializationActor);
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            var completedCount = await dbContext.IngestAttempts
                .CountAsync(attempt => attempt.State == IngestState.Completed);

            var documentCount = await dbContext.IngestedDocuments.CountAsync();
            var chunkCount = await dbContext.DocumentChunks.CountAsync();
            var runCount = await dbContext.AgentRuns.CountAsync();
            var toolCallCount = await dbContext.AgentToolCalls.CountAsync();
            var lengths = await dbContext.DocumentChunks.Select(chunk => chunk.Embedding.Length).ToListAsync();
            var vectors = lengths.Distinct().ToList();

            return new InsightsCounts(completedCount, documentCount, chunkCount, runCount, toolCallCount, vectors);
        }

        public async Task<int> CountPendingMachineDraftsAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .MachineDrafts
                .CountAsync(draft => draft.State == DraftState.Pending);
        }

        public async Task<TelemetryDraftCounts> CountPendingDefinitionDraftsAsync()
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
            var pending = DefinitionDraftState.Pending;
            var metricCount = await dbContext.MetricDefinitionDrafts.CountAsync(draft => draft.State == pending);
            var statusCount = await dbContext.StatusDefinitionDrafts.CountAsync(draft => draft.State == pending);
            var phaseCount = await dbContext.OperatingPhaseDrafts.CountAsync(draft => draft.State == pending);
            var entryCount = await dbContext.ProposalEntries.CountAsync();

            return new TelemetryDraftCounts(metricCount, statusCount, phaseCount, entryCount);
        }
    }
}

public sealed record InsightsCounts(
    int CompletedAttempts,
    int Documents,
    int Chunks,
    int Runs,
    int ToolCalls,
    IReadOnlyList<int> VectorLengths
);

public sealed record TelemetryDraftCounts(int Metrics, int Statuses, int Phases, int Entries);
