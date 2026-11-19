using System.Collections.Concurrent;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete.Dependents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

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
    private static readonly ConcurrentDictionary<Type, IDependentSoftDelete> Handlers = new();

    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ICurrentUserProvider _currentUserProvider = currentUserProvider;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        if (eventData.Context is not null)
        {
            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
            var userId = _currentUserProvider.TryGetUserId();
            Apply(eventData.Context, utcNow, userId);
            FlipDeletedEntries(eventData.Context, utcNow, userId);
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
            await ApplyAsync(eventData.Context, utcNow, userId, cancellationToken);
            FlipDeletedEntries(eventData.Context, utcNow, userId);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>Tells whether a tracked entry is a soft-deleted principal with at least one cascade relation.</summary>
    /// <param name="dbContext">The context that is about to save.</param>
    /// <returns><see langword="true"/> when the cascade has work to do.</returns>
    internal static bool HasCascadeWork(DbContext dbContext) =>
        FindDeletedPrincipals(dbContext).Any(entry => FindCascadeRelations(entry.Metadata).Any());

    /// <summary>
    /// Soft-deletes the untracked rows of each cascade relation of the deleted principals, and the rows below them,
    /// with <c>ExecuteUpdate</c> for each relation. A row that is already soft-deleted keeps its delete time.
    /// </summary>
    /// <param name="dbContext">The context that is about to save. It needs an open transaction.</param>
    /// <param name="deletedAt">The delete time, the same as the one of the tracked entries.</param>
    /// <param name="deletedBy">The actor, the same as the one of the tracked entries.</param>
    /// <param name="cancellationToken">Cancels the statements. The transaction of the save rolls them back.</param>
    /// <exception cref="InvalidOperationException">The context has no open transaction, or a cascade relation uses a
    /// key that is not a single <see cref="Guid"/> column.</exception>
    private static async Task ApplyAsync(
        DbContext dbContext,
        DateTime deletedAt,
        Guid? deletedBy,
        CancellationToken cancellationToken
    )
    {
        if (!HasCascadeWork(dbContext))
        {
            return;
        }

        if (dbContext.Database.CurrentTransaction is null)
        {
            throw SoftDeleteCascadeErrors.NoOpenTransaction();
        }

        var steps = new Queue<(IForeignKey Relation, IReadOnlyCollection<Guid> PrincipalKeys)>();
        foreach (var group in CollectPrincipalKeys(dbContext))
        {
            steps.Enqueue((group.Key, group.Value));
        }

        while (steps.TryDequeue(out var step))
        {
            var (handler, deletion) = CreateStep(dbContext, step.Relation, step.PrincipalKeys, deletedAt, deletedBy);
            var keys = await handler.ApplyAsync(deletion, cancellationToken);
            EnqueueNextSteps(steps, step.Relation, keys);
        }
    }

    /// <summary>
    /// Soft-deletes the untracked rows of each cascade relation of the deleted principals, and the rows below them,
    /// with <c>ExecuteUpdate</c> for each relation. It is <c>ApplyAsync</c> with the synchronous EF Core calls.
    /// </summary>
    /// <param name="dbContext">The context that is about to save. It needs an open transaction.</param>
    /// <param name="deletedAt">The delete time, the same as the one of the tracked entries.</param>
    /// <param name="deletedBy">The actor, the same as the one of the tracked entries.</param>
    /// <exception cref="InvalidOperationException">The context has no open transaction, or a cascade relation uses a
    /// key that is not a single <see cref="Guid"/> column.</exception>
    private static void Apply(DbContext dbContext, DateTime deletedAt, Guid? deletedBy)
    {
        if (!HasCascadeWork(dbContext))
        {
            return;
        }

        if (dbContext.Database.CurrentTransaction is null)
        {
            throw SoftDeleteCascadeErrors.NoOpenTransaction();
        }

        var steps = new Queue<(IForeignKey Relation, IReadOnlyCollection<Guid> PrincipalKeys)>();
        foreach (var group in CollectPrincipalKeys(dbContext))
        {
            steps.Enqueue((group.Key, group.Value));
        }

        while (steps.TryDequeue(out var step))
        {
            var (handler, deletion) = CreateStep(dbContext, step.Relation, step.PrincipalKeys, deletedAt, deletedBy);
            var keys = handler.Apply(deletion);
            EnqueueNextSteps(steps, step.Relation, keys);
        }
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

    private static List<EntityEntry> FindDeletedPrincipals(DbContext dbContext) =>
        dbContext.ChangeTracker.Entries()
            .Where(entry => entry.Entity is ISoftDeletable && IsDeleted(entry))
            .ToList();

    private static bool IsDeleted(EntityEntry entry)
    {
        if (entry.State == EntityState.Deleted)
        {
            return true;
        }

        var deletedAt = entry.Property(nameof(ISoftDeletable.DeletedAt));

        return entry.State == EntityState.Modified
            && deletedAt.OriginalValue is null
            && deletedAt.CurrentValue is not null;
    }

    private static IEnumerable<IForeignKey> FindCascadeRelations(IEntityType principal) =>
        principal.GetReferencingForeignKeys()
            .Where(relation => relation is { DeleteBehavior: DeleteBehavior.Cascade, IsOwnership: false }
                && typeof(ISoftDeletable).IsAssignableFrom(relation.DeclaringEntityType.ClrType));

    private static Dictionary<IForeignKey, List<Guid>> CollectPrincipalKeys(DbContext dbContext)
    {
        var keysByRelation = new Dictionary<IForeignKey, List<Guid>>();
        foreach (var entry in FindDeletedPrincipals(dbContext))
        {
            foreach (var relation in FindCascadeRelations(entry.Metadata))
            {
                EnsureSupportedKeys(relation);
                var key = (Guid)entry.Property(relation.PrincipalKey.Properties[index: 0].Name).CurrentValue!;
                if (!keysByRelation.TryGetValue(relation, out var keys))
                {
                    keys = [];
                    keysByRelation[relation] = keys;
                }

                keys.Add(key);
            }
        }

        return keysByRelation;
    }

    private static void EnsureSupportedKeys(IForeignKey relation)
    {
        var dependent = relation.DeclaringEntityType;
        var isSupported = IsSingleGuid(relation.Properties)
            && IsSingleGuid(relation.PrincipalKey.Properties)
            && relation.PrincipalKey == relation.PrincipalEntityType.FindPrimaryKey()
            && IsSingleGuid(dependent.FindPrimaryKey()?.Properties);
        if (!isSupported)
        {
            throw SoftDeleteCascadeErrors.UnsupportedKey(
                $"{dependent.ClrType.Name}.{relation.Properties[index: 0].Name}");
        }
    }

    private static bool IsSingleGuid(IReadOnlyList<IProperty>? properties) =>
        properties is [{ } property]
        && (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(Guid);

    private static (IDependentSoftDelete Handler, DependentDeletionContext Deletion) CreateStep(
        DbContext dbContext,
        IForeignKey relation,
        IReadOnlyCollection<Guid> principalKeys,
        DateTime deletedAt,
        Guid? deletedBy
    )
    {
        var dependent = relation.DeclaringEntityType;
        var primaryKeyName = dependent.FindPrimaryKey()!.Properties[index: 0].Name;
        var handler = Handlers.GetOrAdd(
            dependent.ClrType,
            clrType => (IDependentSoftDelete)Activator.CreateInstance(
                typeof(DependentSoftDelete<>).MakeGenericType(clrType))!);
        var deletion = new DependentDeletionContext
        {
            DbContext = dbContext,
            Relation = relation,
            PrincipalKeys = principalKeys,
            TrackedKeys = dbContext.ChangeTracker.Entries()
                .Where(entry => dependent.IsAssignableFrom(entry.Metadata))
                .Select(entry => (Guid)entry.Property(primaryKeyName).CurrentValue!)
                .ToArray(),
            HasOwnCascade = FindCascadeRelations(dependent).Any(),
            DeletedAt = deletedAt,
            DeletedBy = deletedBy,
        };

        return (handler, deletion);
    }

    private static void EnqueueNextSteps(
        Queue<(IForeignKey Relation, IReadOnlyCollection<Guid> PrincipalKeys)> steps,
        IForeignKey relation,
        IReadOnlyCollection<Guid> keys
    )
    {
        foreach (var next in FindCascadeRelations(relation.DeclaringEntityType))
        {
            EnsureSupportedKeys(next);
            if (keys.Count > 0)
            {
                steps.Enqueue((next, keys));
            }
        }
    }
}
