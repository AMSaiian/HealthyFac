using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace HealthyFac.Shared.Infrastructure.Persistence.SoftDelete.Dependents;

/// <summary>
/// Soft-deletes rows of <typeparamref name="TDependent"/> with <c>ExecuteUpdate</c>. The soft delete query filter
/// adds <c>DeletedAt IS NULL</c> to each statement, so a row that is already deleted keeps its delete time, also
/// when another transaction deletes it between the key query and the update.
/// </summary>
/// <typeparam name="TDependent">The dependent entity type.</typeparam>
internal sealed class DependentSoftDelete<TDependent> : IDependentSoftDelete
    where TDependent : class, ISoftDeletable
{
    public async Task<List<Guid>> ApplyAsync(
        DbContext dbContext,
        DependentDeletionContext deletion,
        CancellationToken cancellationToken
    )
    {
        var primaryKeyName = deletion.Relation.DeclaringEntityType.FindPrimaryKey()!.Properties[0].Name;
        var rows = FindRows(dbContext, deletion);

        if (!deletion.HasOwnCascade)
        {
            await rows.ExecuteUpdateAsync(setters => SetDeleted(setters, deletion), cancellationToken);

            return [];
        }

        var keys = await rows.Select(row => EF.Property<Guid>(row, primaryKeyName)).ToListAsync(cancellationToken);

        if (keys.Count == 0)
        {
            return keys;
        }

        await FindRowsByKeys(dbContext, deletion, keys)
            .ExecuteUpdateAsync(setters => SetDeleted(setters, deletion), cancellationToken);

        return keys;
    }

    public List<Guid> Apply(DbContext dbContext, DependentDeletionContext deletion)
    {
        var primaryKeyName = deletion.Relation.DeclaringEntityType.FindPrimaryKey()!.Properties[0].Name;
        var rows = FindRows(dbContext, deletion);

        if (!deletion.HasOwnCascade)
        {
            rows.ExecuteUpdate(setters => SetDeleted(setters, deletion));

            return [];
        }

        var keys = rows.Select(row => EF.Property<Guid>(row, primaryKeyName)).ToList();

        if (keys.Count == 0)
        {
            return keys;
        }

        FindRowsByKeys(dbContext, deletion, keys).ExecuteUpdate(setters => SetDeleted(setters, deletion));

        return keys;
    }

    private static IQueryable<TDependent> FindRows(DbContext dbContext, DependentDeletionContext deletion)
    {
        var foreignKeyName = deletion.Relation.Properties[0].Name;
        var primaryKeyName = deletion.Relation.DeclaringEntityType.FindPrimaryKey()!.Properties[0].Name;
        var principals = deletion.PrincipalKeys.ToList();
        var tracked = deletion.TrackedKeys.ToList();

        return dbContext.Set<TDependent>()
            .Where(row => principals.Contains(EF.Property<Guid>(row, foreignKeyName))
                && !tracked.Contains(EF.Property<Guid>(row, primaryKeyName)));
    }

    private static IQueryable<TDependent> FindRowsByKeys(
        DbContext dbContext,
        DependentDeletionContext deletion,
        List<Guid> keys
    )
    {
        var primaryKeyName = deletion.Relation.DeclaringEntityType.FindPrimaryKey()!.Properties[0].Name;

        return dbContext.Set<TDependent>()
            .Where(row => keys.Contains(EF.Property<Guid>(row, primaryKeyName)));
    }

    private static void SetDeleted(UpdateSettersBuilder<TDependent> setters, DependentDeletionContext deletion)
    {
        setters.SetProperty(row => EF.Property<DateTime?>(row, nameof(ISoftDeletable.DeletedAt)), deletion.DeletedAt);
        setters.SetProperty(row => EF.Property<Guid?>(row, nameof(ISoftDeletable.DeletedBy)), deletion.DeletedBy);

        if (typeof(IAuditable).IsAssignableFrom(typeof(TDependent)))
        {
            setters.SetProperty(row => EF.Property<DateTime?>(row, nameof(IAuditable.UpdatedAt)), deletion.DeletedAt);
            setters.SetProperty(row => EF.Property<Guid?>(row, nameof(IAuditable.UpdatedBy)), deletion.DeletedBy);
        }
    }
}
