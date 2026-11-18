using FluentValidation.Results;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

/// <summary>One draft that a definition write names, and the request path of its <c>draftId</c> for an error.</summary>
public sealed record DraftReference
{
    /// <summary>The path of the field in the request, for example <c>created[2].draftId</c>.</summary>
    public required string Path { get; init; }

    public required Guid DraftId { get; init; }
}

/// <summary>
/// Marks the drafts that a definition write names as applied. The four definition writes (metric apply, status
/// create, status update, status batch apply) call it before their one save, so the draft and the definition change
/// together. This is the one place that holds the rules for a usable draft.
/// </summary>
/// <param name="dbContext">Loads the drafts tracked, so the save stores the new state.</param>
public sealed class AppliedDraftMarker(ITelemetryDbContext dbContext)
{
    /// <summary>The request path of the <c>draftId</c> of a write that names one draft.</summary>
    public const string SingleDraftPath = "draftId";

    private readonly ITelemetryDbContext _dbContext = dbContext;

    /// <summary>
    /// Marks metric drafts as applied.
    /// </summary>
    /// <param name="companyId">The company of the actor.</param>
    /// <param name="machineId">The machine that the definitions belong to.</param>
    /// <param name="references">The drafts that the write names.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>A task that completes when the drafts are marked in the context.</returns>
    /// <exception cref="AppValidationException">
    /// Thrown with the path of the item when a draft does not exist, belongs to another company or machine, is a
    /// draft of the other kind, or is named twice.
    /// </exception>
    /// <exception cref="UnprocessableException">Thrown when a draft is not pending.</exception>
    public Task MarkMetricDraftsAsync(
        Guid companyId,
        Guid machineId,
        IReadOnlyList<DraftReference> references,
        CancellationToken cancellationToken
    ) =>
        MarkAsync(_dbContext.MetricDefinitionDrafts, companyId, machineId, references, cancellationToken);

    /// <summary>
    /// Marks status drafts as applied.
    /// </summary>
    /// <param name="companyId">The company of the actor.</param>
    /// <param name="machineId">The machine that the definitions belong to.</param>
    /// <param name="references">The drafts that the write names.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>A task that completes when the drafts are marked in the context.</returns>
    /// <exception cref="AppValidationException">
    /// Thrown with the path of the item when a draft does not exist, belongs to another company or machine, is a
    /// draft of the other kind, or is named twice.
    /// </exception>
    /// <exception cref="UnprocessableException">Thrown when a draft is not pending.</exception>
    public Task MarkStatusDraftsAsync(
        Guid companyId,
        Guid machineId,
        IReadOnlyList<DraftReference> references,
        CancellationToken cancellationToken
    ) =>
        MarkAsync(_dbContext.StatusDefinitionDrafts, companyId, machineId, references, cancellationToken);

    private static async Task MarkAsync<TDraft>(
        DbSet<TDraft> drafts,
        Guid companyId,
        Guid machineId,
        IReadOnlyList<DraftReference> references,
        CancellationToken cancellationToken
    )
        where TDraft : class, IDefinitionDraft
    {
        if (references.Count == 0)
        {
            return;
        }

        var ids = references.Select(reference => reference.DraftId).Distinct().ToList();
        var stored = await drafts
            .Where(draft => draft.CompanyId == companyId && ids.Contains(draft.Id))
            .ToDictionaryAsync(draft => draft.Id, cancellationToken);
        var seenIds = new HashSet<Guid>();

        foreach (var reference in references)
        {
            var isUsable = stored.TryGetValue(reference.DraftId, out var draft)
                && draft.TargetMachineId == machineId
                && seenIds.Add(reference.DraftId);

            if (!isUsable)
            {
                throw new AppValidationException(
                    [new ValidationFailure(reference.Path, DefinitionDraftErrors.DraftNotUsable)]);
            }
        }

        if (stored.Values.Any(draft => draft.State != DefinitionDraftState.Pending))
        {
            throw new UnprocessableException(DefinitionDraftErrors.DraftNotPending);
        }

        foreach (var draft in stored.Values)
        {
            draft.MarkApplied();
        }
    }
}
