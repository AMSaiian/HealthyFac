using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;

/// <summary>
/// Turns the removal of an <see cref="ISoftDeletable"/> entity into an update that sets <c>DeletedAt</c> and
/// <c>DeletedBy</c>. Register it before <see cref="AuditableEntityInterceptor"/>, so the audit interceptor sees the
/// entry as modified and sets <c>UpdatedAt</c> and <c>UpdatedBy</c>.
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
        ApplySoftDelete(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        ApplySoftDelete(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplySoftDelete(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var userId = _currentUserProvider.TryGetUserId();
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

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
