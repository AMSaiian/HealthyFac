using System.Text.Json;
using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Infrastructure.Files.Storage;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Document.Infrastructure.Database.Seeding;

/// <summary>
/// Creates the demo files of the ingest snapshot: the file refs of <c>document.json</c> and their bytes in blob
/// storage. The files belong to the test company and are created as the test supervisor. The seeder skips a file ref
/// whose ID exists, a deleted one too, so a restart adds nothing and a deleted demo file stays deleted.
/// </summary>
/// <remarks>
/// The blob write comes after the save and follows the Resume form of the cancellation rule: a failed upload leaves a
/// row without a blob, and the next start writes the missing blob, because the seeder writes every blob that does not
/// exist. The seeder adds no outbox row: Insights and Core seed their own parts of the snapshot.
/// </remarks>
/// <param name="scopeFactory">Creates the scope of the supervisor.</param>
/// <param name="seedingOptions">The validated seeding options.</param>
/// <param name="timeProvider">Provides the seed time.</param>
/// <param name="location">The folder of the snapshot.</param>
public sealed class DemoFileSeeder(
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
    /// Adds the missing file refs in one save, then writes the missing blobs.
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

        var seedFile = await ReadSeedFileAsync(cancellationToken);
        var shift = SeedTimeShift.Create(seedFile.ReferenceTime, _timeProvider);
        using var scope = _scopeFactory.CreateActorScope(
            new ActorContext
            {
                Kind = ActorKind.User,
                UserId = tenant.SupervisorIdentityId,
                CompanyId = tenant.CompanyId,
            });
        var dbContext = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<BlobFileStorage>();

        var seedIds = seedFile.FileRefs.Select(row => row.Id).ToList();
        var existingIds = await dbContext.FileRefs
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(fileRef => seedIds.Contains(fileRef.Id))
            .Select(fileRef => fileRef.Id)
            .ToListAsync(cancellationToken);

        foreach (var row in seedFile.FileRefs.Where(row => !existingIds.Contains(row.Id)))
        {
            Add(dbContext, row, tenant.CompanyId, shift);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var row in seedFile.FileRefs)
        {
            await WriteBlobAsync(storage, row, cancellationToken);
        }
    }

    private static void Add(DocumentDbContext dbContext, DemoFileSeedRow row, Guid companyId, SeedTimeShift shift)
    {
        var entry = dbContext.FileRefs.Add(
            new FileRef(
                companyId,
                row.MachineId,
                FilePurpose.FromValue(row.Purpose),
                row.FileName,
                row.Extension,
                row.ContentType,
                row.SizeBytes,
                Convert.FromHexString(row.Sha256),
                row.Description));
        entry.Property(nameof(FileRef.Id)).CurrentValue = row.Id;
        entry.Property(nameof(FileRef.IngestState)).CurrentValue = FileIngestState.FromValue(row.IngestState);
        entry.Property(nameof(FileRef.IngestFailedCount)).CurrentValue = row.IngestFailedCount;
        entry.Property(nameof(FileRef.IngestStateChangedAt)).CurrentValue = shift.Shift(row.IngestStateChangedAt);
        entry.Property(nameof(FileRef.MachineChangedAt)).CurrentValue = shift.Shift(row.MachineChangedAt);
    }

    private async Task WriteBlobAsync(
        BlobFileStorage storage,
        DemoFileSeedRow row,
        CancellationToken cancellationToken
    )
    {
        var isStored = await storage.ExistsAsync(row.Id, cancellationToken);
        if (isStored)
        {
            return;
        }

        var type = AllowedFileTypes.FindByExtension(row.Extension)
            ?? throw new InvalidOperationException(DemoFileSeedErrors.UnsupportedExtension(row.Id));
        await using var stream = File.OpenRead(
            Path.Combine(_location.Folder, DemoIngestSeedFiles.FilesFolderName, $"{row.FileName}.{row.Extension}"));

        await storage.WriteAsync(
            row.Id,
            FilePurpose.FromValue(row.Purpose),
            stream,
            row.ContentType,
            FileContentDisposition.Build(type, row.FileName),
            cancellationToken);
    }

    private async Task<DemoDocumentSeedFile> ReadSeedFileAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(Path.Combine(_location.Folder, DemoIngestSeedFiles.DocumentFileName));
        var seedFile = await JsonSerializer.DeserializeAsync<DemoDocumentSeedFile>(
            stream,
            JsonOptions,
            cancellationToken);

        return seedFile ?? throw new InvalidOperationException(DemoFileSeedErrors.SeedFileEmpty);
    }
}
