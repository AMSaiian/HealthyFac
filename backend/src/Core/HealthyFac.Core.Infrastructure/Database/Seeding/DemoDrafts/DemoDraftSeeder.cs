using System.Text.Json;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Drafts;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.Infrastructure.Database.Seeding.DemoDrafts;

/// <summary>
/// Creates the demo machine drafts and the pending work order draft of the ingest snapshot from <c>core.json</c>
/// (Telemetry seeds the metric and status drafts, and Insights seeds the case that the work order draft links). The
/// rows belong to the test company and are created as the test supervisor. The seeder skips a draft whose ID exists,
/// a deleted one too, so a restart adds nothing.
/// </summary>
/// <remarks>
/// One <c>SaveChangesAsync</c> stores every row. The seeder adds no outbox row: the drafts come from the ingest in
/// Insights, and Document and Insights seed the files and the attempts themselves.
/// </remarks>
/// <param name="scopeFactory">Creates the scope of the supervisor.</param>
/// <param name="seedingOptions">The validated seeding options.</param>
/// <param name="timeProvider">Provides the seed time.</param>
/// <param name="location">The folder of the snapshot.</param>
public sealed class DemoDraftSeeder(
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
    /// Adds the missing machine drafts and work order drafts in one save.
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
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var machineIds = seedFile.MachineDrafts.Select(row => row.Id).ToList();
        var existingMachineIds = await dbContext.MachineDrafts
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(draft => machineIds.Contains(draft.Id))
            .Select(draft => draft.Id)
            .ToListAsync(cancellationToken);

        dbContext.MachineDrafts.AddRange(
            seedFile.MachineDrafts
                .Where(row => !existingMachineIds.Contains(row.Id))
                .Select(row => ToMachineDraft(row, tenant.CompanyId, shift)));

        var workOrderIds = seedFile.WorkOrderDrafts.Select(row => row.Id).ToList();
        var existingWorkOrderIds = await dbContext.WorkOrderDrafts
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(draft => workOrderIds.Contains(draft.Id))
            .Select(draft => draft.Id)
            .ToListAsync(cancellationToken);

        dbContext.WorkOrderDrafts.AddRange(
            seedFile.WorkOrderDrafts
                .Where(row => !existingWorkOrderIds.Contains(row.Id))
                .Select(row => ToWorkOrderDraft(row, tenant.CompanyId)));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static WorkOrderDraft ToWorkOrderDraft(DemoWorkOrderDraftRow row, Guid companyId) =>
        new(
            row.Id,
            companyId,
            row.MachineId,
            row.CaseId,
            row.RunId,
            row.Title,
            row.Description,
            WorkOrderPriority.FromValue(row.Priority),
            WorkOrderCategory.FromValue(row.Category),
            row.EstimateMinutes);

    private static MachineDraft ToMachineDraft(DemoMachineDraftRow row, Guid companyId, SeedTimeShift shift) =>
        new(
            row.Id,
            companyId,
            row.AttemptId,
            row.FileId,
            shift.Shift(row.QueuedAt)!.Value,
            row.TargetMachineId,
            row.Name,
            row.SerialNumber,
            row.Description);

    private async Task<DemoDraftSeedFile> ReadSeedFileAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(Path.Combine(_location.Folder, DemoIngestSeedFiles.CoreFileName));
        var seedFile = await JsonSerializer.DeserializeAsync<DemoDraftSeedFile>(stream, JsonOptions, cancellationToken);

        return seedFile ?? throw new InvalidOperationException(DemoDraftSeedErrors.SeedFileEmpty);
    }
}
