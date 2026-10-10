using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using HealthyFac.Shared.Application.Interfaces;
using HealthyFac.Shared.Domain.Interfaces;

namespace HealthyFac.Shared.Infrastructure.Persistence;

public sealed class AuditableEntityInterceptor(
    IDateProvider dateProvider,
    ICurrentUserProvider currentUserProvider
) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        ApplyAudit(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        ApplyAudit(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var userId = currentUserProvider.TryGetUserId() ?? Guid.Empty;
        var utcNow = dateProvider.GetUtc();

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(auditable => auditable.CreatedAt).CurrentValue = utcNow;
                if (userId != Guid.Empty)
                {
                    entry.Property(auditable => auditable.CreatedBy).CurrentValue = userId;
                }
            }

            if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Property(auditable => auditable.UpdatedAt).CurrentValue = utcNow;
                if (userId != Guid.Empty)
                {
                    entry.Property(auditable => auditable.UpdatedBy).CurrentValue = userId;
                }
            }
        }
    }
}

internal static class DbContextExtensions
{
    extension(EntityEntry entry)
    {
        /// <summary>
        /// Tells whether an owned entity of the entry is new or changed, so the owner counts as changed.
        /// </summary>
        /// <returns><see langword="true"/> when at least one owned entity is added or modified.</returns>
        public bool HasChangedOwnedEntities()
            => entry.References.Any(reference =>
                reference.TargetEntry != null
                && reference.TargetEntry.Metadata.IsOwned()
                && reference.TargetEntry.State is EntityState.Added or EntityState.Modified);
    }
}
