using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;

/// <summary>
/// Turns the removal of an <see cref="ISoftDeletable"/> entity into an update that sets <c>DeletedAt</c> and
/// <c>DeletedBy</c>. Register it before <see cref="AuditableEntityInterceptor"/>, so the audit interceptor sees the
/// entry as modified and sets <c>UpdatedAt</c> and <c>UpdatedBy</c>. It also soft-deletes the untracked rows of the
/// <c>Cascade</c> relations of a deleted entity, read from the model; <c>Restrict</c> relations and dependents without
/// <see cref="ISoftDeletable"/> stay unchanged.
/// </summary>
public sealed class SoftDeleteInterceptor(
    TimeProvider timeProvider,
    ICurrentUserProvider currentUserProvider
) : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ICurrentUserProvider _currentUserProvider = currentUserProvider;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        if (eventData.Context is not null && SoftDeleteCascade.HasCascadeWork(eventData.Context))
        {
            throw SoftDeleteCascadeErrors.SynchronousSave();
        }

        if (eventData.Context is not null)
        {
            FlipDeletedEntries(
                eventData.Context, _timeProvider.GetUtcNow().UtcDateTime, _currentUserProvider.TryGetUserId());
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (eventData.Context is not null)
        {
            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
            var userId = _currentUserProvider.TryGetUserId();
            await SoftDeleteCascade.ApplyAsync(eventData.Context, utcNow, userId, cancellationToken);
            FlipDeletedEntries(eventData.Context, utcNow, userId);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void FlipDeletedEntries(DbContext context, DateTime utcNow, Guid? userId)
    {
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>()
            .Where(entry => entry.State == EntityState.Deleted)
            .ToList())
        {
            entry.State = EntityState.Modified;
            entry.Property(deletable => deletable.DeletedAt).CurrentValue = utcNow;
            entry.Property(deletable => deletable.DeletedBy).CurrentValue = userId;
        }
    }
}
