using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Seeder.Ingest;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.ServiceHosts;
using HealthyFac.Seeder.TelemetryHistory;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.Timeline;

/// <summary>
/// Tells whether an earlier run seeded everything. The seed is complete when Core holds the machine draft of the last
/// entry of ingest set 2, because that ingest is the last step of the timeline that reaches Core through the relay.
/// The seed is incomplete when the company exists without that draft. The telemetry history adds the received message
/// with the ID of the marker of <c>telemetry.json</c>: with it the seed is complete with the history, and without it
/// the seed is complete without the history, whatever other received messages exist (the live machines write them).
/// A run never repairs an incomplete seed: the reset starts it again.
/// </summary>
public sealed class SeedCompletion
{
    private readonly ServiceHost _core;
    private readonly ServiceHost _telemetry;
    private readonly Guid _lastDraftId;
    private readonly Guid _markerMessageId;

    public SeedCompletion(ServiceHost core, ServiceHost telemetry, Guid lastDraftId, Guid markerMessageId)
    {
        _core = core;
        _telemetry = telemetry;
        _lastDraftId = lastDraftId;
        _markerMessageId = markerMessageId;
    }

    /// <summary>Reads the ID of the received message that marks the telemetry history as complete.</summary>
    /// <param name="folder">The seed folder.</param>
    /// <returns>The message ID.</returns>
    public static Guid FindMarkerMessageId(SeedFolder folder) =>
        folder.Read<TelemetryFile>(TelemetryHistoryConstants.FileName).MarkerMessageId;

    /// <summary>Reads the ID of the machine draft of the last entry of the last ingest set.</summary>
    /// <param name="folder">The seed folder.</param>
    /// <returns>The draft ID.</returns>
    public static Guid FindLastDraftId(SeedFolder folder)
    {
        var lastSet = string.Format(IngestConstants.SetFilePattern, IngestConstants.SetNumbers[^1]);

        return folder.Read<IngestSetFile>(lastSet).Entries[^1].DraftsProposed.Machines[index: 0].DraftId;
    }

    /// <summary>
    /// Reads the item of the ingest event of the last entry of the last ingest set. Its draft is the marker.
    /// </summary>
    /// <param name="folder">The seed folder.</param>
    /// <returns>The item name of the event.</returns>
    public static string FindMarkerItem(SeedFolder folder)
    {
        var lastSet = string.Format(IngestConstants.SetFilePattern, IngestConstants.SetNumbers[^1]);
        var lastEntry = folder.Read<IngestSetFile>(lastSet).Entries[^1];

        return SeedItem.Of(lastSet, $"{lastEntry.File.Key}{IngestConstants.IngestSuffix}");
    }

    /// <summary>Reads the state of the seed from Core.</summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The state.</returns>
    public async Task<SeedState> ReadAsync(CancellationToken cancellationToken)
    {
        using var scope = _core.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Core));
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var hasCompany = await dbContext.Companies.AnyAsync(cancellationToken);
        var hasLastDraft = await dbContext.MachineDrafts.AnyAsync(draft => draft.Id == _lastDraftId, cancellationToken);
        if (!hasCompany)
        {
            return SeedState.Empty;
        }

        if (!hasLastDraft)
        {
            return SeedState.Incomplete;
        }

        using var telemetryScope =
            _telemetry.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Telemetry));
        var messages = telemetryScope.ServiceProvider.GetRequiredService<TelemetryDbContext>().ReceivedMessages;
        var hasMarkerMessage = await messages.AnyAsync(message => message.Id == _markerMessageId, cancellationToken);

        return hasMarkerMessage ? SeedState.CompleteWithHistory : SeedState.Complete;
    }
}
