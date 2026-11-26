using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.StatusDrafts;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts.Following;

/// <summary>
/// The one place where the handlers of the proposal messages find an entry and move its draft children. The messages
/// of one entry arrive in any order, so each handler finds or creates the entry, changes it, and then lets the
/// children follow it.
/// </summary>
/// <param name="dbContext">Reads the entries, the children, and the registrations.</param>
/// <param name="registrationLock">Locks the registration of the machine before the handler reads it.</param>
public sealed class ProposalEntries(ITelemetryDbContext dbContext, IMachineRegistrationLock registrationLock)
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly IMachineRegistrationLock _registrationLock = registrationLock;

    /// <summary>
    /// Finds the entry, also when it is soft-deleted, or adds a new <c>awaiting</c> entry.
    /// </summary>
    /// <param name="entryId">The ID of the proposed machine.</param>
    /// <param name="companyId">The company of the proposal.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The tracked entry.</returns>
    public async Task<ProposalEntry> FindOrCreateAsync(
        Guid entryId,
        Guid companyId,
        CancellationToken cancellationToken
    )
    {
        var entry = await _dbContext.ProposalEntries
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .SingleOrDefaultAsync(stored => stored.Id == entryId, cancellationToken);

        if (entry is not null)
        {
            return entry;
        }

        var created = new ProposalEntry(entryId, companyId);
        _dbContext.ProposalEntries.Add(created);

        return created;
    }

    /// <summary>
    /// Moves every child of the entry that waits (<c>awaitingTarget</c> or <c>pending</c>) to the target that the
    /// entry names, and removes the children that the entry asks to remove.
    /// </summary>
    /// <param name="entry">The entry that changed.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    public async Task FollowAsync(ProposalEntry entry, CancellationToken cancellationToken)
    {
        var isTargetRegistrationDeleted = await IsTargetRegistrationDeletedAsync(entry, cancellationToken);

        var metrics = await _dbContext.MetricDefinitionDrafts
            .Where(draft => draft.ProposalEntryId == entry.Id)
            .Where(OpenDefinitionDrafts.Predicate<MetricDefinitionDraft>())
            .ToListAsync(cancellationToken);

        var statuses = await _dbContext.StatusDefinitionDrafts
            .Where(draft => draft.ProposalEntryId == entry.Id)
            .Where(OpenDefinitionDrafts.Predicate<StatusDefinitionDraft>())
            .ToListAsync(cancellationToken);

        var phases = await _dbContext.OperatingPhaseDrafts
            .Include(draft => draft.Conditions)
            .Include(draft => draft.Limits)
            .Where(draft => draft.ProposalEntryId == entry.Id)
            .Where(OpenDefinitionDrafts.Predicate<OperatingPhaseDraft>())
            .ToListAsync(cancellationToken);

        _dbContext.MetricDefinitionDrafts.RemoveRange(
            metrics.Where(metric => metric.Follow(entry, isTargetRegistrationDeleted)));

        _dbContext.StatusDefinitionDrafts.RemoveRange(
            statuses.Where(status => status.Follow(entry, isTargetRegistrationDeleted)));

        _dbContext.OperatingPhaseDrafts.RemoveRange(
            phases.Where(phase => phase.Follow(entry, isTargetRegistrationDeleted)));
    }

    /// <summary>
    /// Makes a draft that was just added follow its entry. The entry state is known, but the draft is not stored yet.
    /// </summary>
    /// <param name="draft">The new draft.</param>
    /// <param name="entry">The entry of the draft.</param>
    /// <param name="isTargetRegistrationDeleted">
    /// The answer of <see cref="IsTargetRegistrationDeletedAsync"/> for the entry, read once for all its new drafts.
    /// </param>
    /// <returns><see langword="true"/> when the draft must not be stored (the proposal was rejected).</returns>
    public static bool FollowNewDraft(IDefinitionDraft draft, ProposalEntry entry, bool isTargetRegistrationDeleted) =>
        draft.Follow(entry, isTargetRegistrationDeleted);

    /// <summary>
    /// Makes the save check the row version of an entry that already exists. A handler that adds children to an entry
    /// without changing it calls this, so a parallel handler that changes the entry makes one of the two saves fail
    /// (the inbox then retries it) and no child is left behind a state change.
    /// </summary>
    /// <param name="entry">The entry that the handler found or created.</param>
    public void EnsureRowVersionChecked(ProposalEntry entry)
    {
        var entityEntry = _dbContext.Entry(entry);

        if (entityEntry.State == EntityState.Unchanged)
        {
            entityEntry.State = EntityState.Modified;
        }
    }

    /// <summary>
    /// Reads whether the machine of an <c>onMachine</c> entry has a soft-deleted registration. It first locks the live
    /// registration (<see cref="IMachineRegistrationLock"/>) as a wait point: a parallel <c>MachineDeleted</c> of the
    /// same machine ends first, and the read then sees <c>DeletedAt</c>, because a soft-deleted row no longer matches
    /// the lock query. Call it with the entries in ascending machine ID order, so 2 messages never lock in opposite
    /// orders.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns><see langword="false"/> when the entry has no machine, or the registration is missing.</returns>
    public async Task<bool> IsTargetRegistrationDeletedAsync(ProposalEntry entry, CancellationToken cancellationToken)
    {
        if (entry.MachineId is not { } machineId)
        {
            return false;
        }

        await _registrationLock.LockLiveAsync(machineId, cancellationToken);

        return await _dbContext.MachineRegistrations
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AsNoTracking()
            .AnyAsync(
                registration => registration.Id == machineId && registration.DeletedAt != null, cancellationToken);
    }
}
