using System.Runtime.InteropServices;
using System.Text.Json;
using HealthyFac.Insights.Domain.Documents;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Database.Seeding;

/// <summary>
/// Creates the demo ingest results: the completed attempts, the ingested documents, and the chunks with their
/// vectors, from <c>insights.json</c> and <c>insights-embeddings.json</c>. The rows belong to the test company and are
/// created as the test supervisor. The seeder skips an attempt or a document whose ID exists, so a restart adds
/// nothing. A document brings all its chunks, so a skipped document skips its chunks too.
/// </summary>
/// <remarks>
/// One <c>SaveChangesAsync</c> stores every row, so a failure leaves nothing half stored. The seeder adds no outbox
/// row: Document and Core seed their own parts of the snapshot, and the worker never claims a completed attempt.
/// </remarks>
/// <param name="scopeFactory">Creates the scope of the supervisor.</param>
/// <param name="seedingOptions">The validated seeding options.</param>
/// <param name="timeProvider">Provides the seed time.</param>
/// <param name="location">The folder of the snapshot.</param>
public sealed class DemoIngestSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<SeedingOptions> seedingOptions,
    TimeProvider timeProvider,
    DemoIngestSeedLocation location
)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly DevelopmentDataOptions _options = seedingOptions.Value.DevelopmentData;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly DemoIngestSeedLocation _location = location;

    /// <summary>
    /// Adds the missing attempts and documents with their chunks in one save.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (_options.TestTenant is not { } tenant)
        {
            return;
        }

        var seedFile = await ReadAsync<DemoIngestSeedFile>(DemoIngestSeedFiles.InsightsFileName, cancellationToken);
        var embeddingsFile = await ReadAsync<DemoEmbeddingsFile>(
            DemoIngestSeedFiles.EmbeddingsFileName,
            cancellationToken);
        var shift = SeedTimeShift.Create(seedFile.ReferenceTime, _timeProvider);
        using var scope = _scopeFactory.CreateActorScope(
            new ActorContext
            {
                Kind = ActorKind.User,
                UserId = tenant.SupervisorIdentityId,
                CompanyId = tenant.CompanyId,
            });
        var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

        var attemptIds = seedFile.IngestAttempts.Select(row => row.Id).ToList();
        var existingAttemptIds = await dbContext.IngestAttempts
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(attempt => attemptIds.Contains(attempt.Id))
            .Select(attempt => attempt.Id)
            .ToListAsync(cancellationToken);

        foreach (var row in seedFile.IngestAttempts.Where(row => !existingAttemptIds.Contains(row.Id)))
        {
            AddAttempt(dbContext, row, tenant.CompanyId, shift);
        }

        foreach (var row in seedFile.IngestedDocuments.Where(row => !existingAttemptIds.Contains(row.AttemptId)))
        {
            AddDocument(dbContext, row, tenant.CompanyId, shift, seedFile.DocumentChunks, embeddingsFile);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void AddAttempt(InsightsDbContext dbContext, DemoAttemptRow row, Guid companyId, SeedTimeShift shift)
    {
        if (!string.Equals(row.State, IngestState.Completed.Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(DemoIngestSeedErrors.AttemptNotCompleted(row.Id));
        }

        var attempt = new IngestAttempt(
            companyId,
            row.FileId,
            row.MachineId,
            shift.Shift(row.MachineChangedAt)!.Value,
            row.FileName,
            row.Extension,
            row.SizeBytes,
            shift.Shift(row.QueuedAt)!.Value,
            traceParent: null);
        var finishedAt = shift.Shift(row.FinishedAt)!.Value;
        attempt.Claim(shift.Shift(row.StartedAt)!.Value, finishedAt);
        attempt.Complete(finishedAt, row.PageCount, row.ChunkCount, row.ProposedDraftCount, warning: null);

        dbContext.IngestAttempts.Add(attempt).Property(nameof(IngestAttempt.Id)).CurrentValue = row.Id;
    }

    private static void AddDocument(
        InsightsDbContext dbContext,
        DemoDocumentRow row,
        Guid companyId,
        SeedTimeShift shift,
        IReadOnlyList<DemoChunkRow> chunkRows,
        DemoEmbeddingsFile embeddingsFile
    )
    {
        var document = new IngestedDocument(
            row.Id,
            companyId,
            row.MachineId,
            shift.Shift(row.MachineChangedAt)!.Value,
            row.AttemptId,
            row.Extension,
            row.PageCount);
        var chunks = chunkRows.Where(chunk => chunk.DocumentId == row.Id).OrderBy(chunk => chunk.Ordinal).ToList();
        foreach (var chunk in chunks)
        {
            document.AddChunk(chunk.Location, chunk.Text, Decode(embeddingsFile.Embeddings[chunk.Id]));
        }

        dbContext.IngestedDocuments.Add(document);
        foreach (var (chunk, chunkRow) in document.Chunks.Zip(chunks))
        {
            dbContext.Entry(chunk).Property(nameof(DocumentChunk.Id)).CurrentValue = chunkRow.Id;
        }
    }

    private static float[] Decode(string base64) =>
        MemoryMarshal.Cast<byte, float>(Convert.FromBase64String(base64)).ToArray();

    private async Task<TFile> ReadAsync<TFile>(string fileName, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(Path.Combine(_location.Folder, fileName));
        var file = await JsonSerializer.DeserializeAsync<TFile>(stream, JsonOptions, cancellationToken);

        return file ?? throw new InvalidOperationException(DemoIngestSeedErrors.SeedFileEmpty(fileName));
    }
}
