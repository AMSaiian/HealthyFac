using System.Text.Json;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Seeding;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>
/// Creates the demo metric and status drafts of the ingest snapshot from <c>telemetry.json</c>, all pending, with one
/// proposal entry for each proposed machine. The rows belong to the test company. The seeder skips a draft or an entry
/// whose ID exists, a deleted one too, so a restart adds nothing.
/// </summary>
/// <remarks>
/// One <c>SaveChangesAsync</c> stores every row. The seeder adds no outbox row: the drafts come from the ingest in
/// Insights. The creation time of a draft is the seed time, because the audit interceptor owns it. The entry ID of a
/// row is the ID of the machine draft in <c>core.json</c> (the file stores it).
/// </remarks>
/// <param name="dbContext">The telemetry database context.</param>
/// <param name="seedingOptions">The validated seeding options.</param>
/// <param name="location">The folder of the snapshot.</param>
public sealed class DemoDefinitionDraftSeeder(
    TelemetryDbContext dbContext,
    IOptions<SeedingOptions> seedingOptions,
    DemoIngestSeedLocation location
)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TelemetryDbContext _dbContext = dbContext;
    private readonly DevelopmentDataOptions _options = seedingOptions.Value.DevelopmentData;
    private readonly DemoIngestSeedLocation _location = location;

    /// <summary>
    /// Adds the missing entries and drafts in one save.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown with <see cref="DemoDefinitionDraftSeedErrors"/> when the file is empty or an entry names no target.
    /// </exception>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (_options.TestTenant is not { } tenant)
        {
            return;
        }

        var seedFile = await ReadSeedFileAsync(cancellationToken);
        var metricIds = seedFile.MetricDefinitionDrafts.Select(row => row.Id).ToList();
        var statusIds = seedFile.StatusDefinitionDrafts.Select(row => row.Id).ToList();
        var entryIds = seedFile.MetricDefinitionDrafts.Select(row => row.ProposalEntryId)
            .Concat(seedFile.StatusDefinitionDrafts.Select(row => row.ProposalEntryId))
            .Distinct()
            .ToList();

        var existingMetricIds = await _dbContext.MetricDefinitionDrafts
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(draft => metricIds.Contains(draft.Id))
            .Select(draft => draft.Id)
            .ToListAsync(cancellationToken);
        var existingStatusIds = await _dbContext.StatusDefinitionDrafts
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(draft => statusIds.Contains(draft.Id))
            .Select(draft => draft.Id)
            .ToListAsync(cancellationToken);
        var entries = await _dbContext.ProposalEntries
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(entry => entryIds.Contains(entry.Id))
            .ToDictionaryAsync(entry => entry.Id, cancellationToken);

        foreach (var entryId in entryIds.Where(entryId => !entries.ContainsKey(entryId)))
        {
            var entry = CreateEntry(entryId, tenant.CompanyId, seedFile);
            entries[entryId] = entry;
            _dbContext.ProposalEntries.Add(entry);
        }

        foreach (var row in seedFile.MetricDefinitionDrafts.Where(row => !existingMetricIds.Contains(row.Id)))
        {
            var draft = MetricDefinitionDraft.Propose(
                row.Id,
                tenant.CompanyId,
                row.AttemptId,
                row.FileId,
                row.ProposalEntryId,
                row.TargetMachineId,
                new MetricDraftValues
                {
                    SourceLocation = row.SourceLocation,
                    Key = row.Key,
                    Name = row.Name,
                    Description = row.Description,
                    ValueKind = row.ValueKind,
                    QuantityKindName = row.QuantityKindName,
                    UnitName = row.UnitName,
                    Min = row.Min,
                    Max = row.Max,
                    States = row.States,
                });
            draft.Follow(entries[row.ProposalEntryId], isTargetRegistrationDeleted: false);
            _dbContext.MetricDefinitionDrafts.Add(draft);
        }

        foreach (var row in seedFile.StatusDefinitionDrafts.Where(row => !existingStatusIds.Contains(row.Id)))
        {
            var draft = StatusDefinitionDraft.Propose(
                row.Id,
                tenant.CompanyId,
                row.AttemptId,
                row.FileId,
                row.ProposalEntryId,
                row.TargetMachineId,
                new StatusDraftValues
                {
                    SourceLocation = row.SourceLocation,
                    Code = row.Code,
                    Severity = row.Severity,
                    Description = row.Description,
                });
            draft.Follow(entries[row.ProposalEntryId], isTargetRegistrationDeleted: false);
            _dbContext.StatusDefinitionDrafts.Add(draft);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ProposalEntry CreateEntry(Guid entryId, Guid companyId, DemoDefinitionDraftSeedFile seedFile)
    {
        var targets = seedFile.MetricDefinitionDrafts
            .Where(row => row.ProposalEntryId == entryId)
            .Select(row => (row.TargetMachineDraftId, row.TargetMachineId))
            .Concat(
                seedFile.StatusDefinitionDrafts
                    .Where(row => row.ProposalEntryId == entryId)
                    .Select(row => (row.TargetMachineDraftId, row.TargetMachineId)))
            .ToList();
        var entry = new ProposalEntry(entryId, companyId);
        var machineId = targets.Select(target => target.TargetMachineId).FirstOrDefault(id => id is not null);
        var isOnMachineDraft = targets.Any(target => target.TargetMachineDraftId is not null);

        if (isOnMachineDraft)
        {
            entry.AcceptOnMachineDraft();
        }
        else
        {
            entry.AcceptOnMachine(
                machineId ?? throw new InvalidOperationException(DemoDefinitionDraftSeedErrors.EntryWithoutTarget));
        }

        return entry;
    }

    private async Task<DemoDefinitionDraftSeedFile> ReadSeedFileAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(Path.Combine(_location.Folder, DemoIngestSeedFiles.TelemetryFileName));
        var seedFile = await JsonSerializer.DeserializeAsync<DemoDefinitionDraftSeedFile>(
            stream,
            JsonOptions,
            cancellationToken);

        return seedFile ?? throw new InvalidOperationException(DemoDefinitionDraftSeedErrors.SeedFileEmpty);
    }
}
