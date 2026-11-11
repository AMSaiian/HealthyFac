using HealthyFac.Insights.Domain.Documents;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Database.Data;

public static class IngestAttemptTestData
{
    public const int EmbeddingDimensions = 1536;

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid FileId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000201");

    public static readonly DateTime Now = new(
        year: 2026, month: 11, day: 9, hour: 12, minute: 0, second: 0,
        DateTimeKind.Utc);

    public static IngestAttempt CreateQueued(Guid? fileId = null) =>
        new(
            CompanyId,
            fileId ?? FileId,
            machineId: null,
            Now,
            fileName: "manual",
            extension: "pdf",
            sizeBytes: 1024,
            Now,
            traceParent: null);

    public static IngestAttempt CreateCompleted(Guid? fileId = null)
    {
        var attempt = CreateQueued(fileId);
        attempt.Claim(Now, Now.AddMinutes(value: 5));
        attempt.Complete(Now, pageCount: 1, chunkCount: 1, draftCount: 0, warning: null);

        return attempt;
    }

    public static IngestAttempt CreateFailed(Guid? fileId = null)
    {
        var attempt = CreateQueued(fileId);
        attempt.Claim(Now, Now.AddMinutes(value: 5));
        attempt.Fail(Now, IngestErrorCode.NoText);

        return attempt;
    }

    public static IngestedDocument CreateDocument(Guid attemptId, Guid? fileId = null)
    {
        var document = new IngestedDocument(
            fileId ?? FileId,
            CompanyId,
            machineId: null,
            Now,
            attemptId,
            extension: "pdf",
            pageCount: 1);
        document.AddChunk(location: "page 1", text: "Text", new float[EmbeddingDimensions]);

        return document;
    }

    extension(InsightsFixture fixture)
    {
        public async Task SaveAsync(params object[] entities)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();
            dbContext.AddRange(entities);

            await dbContext.SaveChangesAsync();
        }

        public async Task<string?> FindIndexDefinitionAsync(string indexName)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            return await dbContext.Database
                .SqlQuery<string>($"SELECT indexdef AS \"Value\" FROM pg_indexes WHERE indexname = {indexName}")
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Waits until a session of the database waits for a row lock, so a race test releases a held save only after
        /// the other call queued behind it. The wait ends with a timeout error after <paramref name="timeout"/>.
        /// </summary>
        public async Task WaitForRowLockWaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limit.CancelAfter(timeout);
            while (true)
            {
                using var scope = fixture.ScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();
                var waiting = await dbContext.Database
                    .SqlQuery<int>(
                        $"SELECT count(*)::int AS \"Value\" FROM pg_stat_activity WHERE wait_event_type = 'Lock'")
                    .SingleAsync(limit.Token);
                if (waiting > 0)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(value: 20), limit.Token);
            }
        }

        public async Task<int> CountChunksAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            return await dbContext.DocumentChunks.CountAsync();
        }

        public async Task DeleteDocumentAsync(Guid documentId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            await dbContext.IngestedDocuments.Where(document => document.Id == documentId).ExecuteDeleteAsync();
        }
    }
}
