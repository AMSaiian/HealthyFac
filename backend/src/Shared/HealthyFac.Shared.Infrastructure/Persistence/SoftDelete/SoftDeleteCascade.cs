using System.Data.Common;
using System.Globalization;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;

/// <summary>
/// Soft-deletes the untracked rows of the <c>Cascade</c> relations of a soft-deleted entity, read from the model. A
/// relation counts only when its dependent type implements <see cref="ISoftDeletable"/>; <c>Restrict</c> relations
/// and other dependents stay unchanged.
/// </summary>
public static class SoftDeleteCascade
{
    private const string PrincipalKeysParameter = "principalKeys";
    private const string TrackedKeysParameter = "trackedKeys";
    private const string KeysParameter = "keys";
    private const string DeletedAtParameter = "deletedAt";
    private const string DeletedByParameter = "deletedBy";
    private const string UpdateByRelationTemplate = "UPDATE {0} SET {1} WHERE {2}";
    private const string UpdateByKeysTemplate = "UPDATE {0} SET {1} WHERE {2} = ANY(@" + KeysParameter + ")";
    private const string SelectKeysTemplate = "SELECT {0} AS \"Value\" FROM {1} WHERE {2}";

    /// <summary>Tells whether a tracked entry is a soft-deleted principal with at least one cascade relation.</summary>
    /// <param name="dbContext">The context that is about to save.</param>
    /// <returns><see langword="true"/> when <see cref="ApplyAsync"/> has work to do.</returns>
    public static bool HasCascadeWork(DbContext dbContext) =>
        FindDeletedPrincipals(dbContext).Any(entry => FindCascadeRelations(entry.Metadata).Any());

    /// <summary>
    /// Soft-deletes the untracked rows of each cascade relation of the deleted principals, and the rows below them,
    /// with one <c>UPDATE</c> for each relation. A row that is already soft-deleted keeps its delete time.
    /// </summary>
    /// <param name="dbContext">The context that is about to save. It needs an open transaction.</param>
    /// <param name="deletedAt">The delete time, the same as the one of the tracked entries.</param>
    /// <param name="deletedBy">The actor, the same as the one of the tracked entries.</param>
    /// <param name="cancellationToken">Cancels the statements. The transaction of the save rolls them back.</param>
    /// <exception cref="InvalidOperationException">The context has no open transaction, or a cascade relation uses a
    /// key that is not a single <see cref="Guid"/> column.</exception>
    public static async Task ApplyAsync(
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
            var keys = await CascadeAsync(
                dbContext, step.Relation, step.PrincipalKeys, deletedAt, deletedBy, cancellationToken);
            foreach (var relation in FindCascadeRelations(step.Relation.DeclaringEntityType))
            {
                EnsureSupportedKeys(relation);
                if (keys.Count > 0)
                {
                    steps.Enqueue((relation, keys));
                }
            }
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

    private static async Task<IReadOnlyCollection<Guid>> CascadeAsync(
        DbContext dbContext,
        IForeignKey relation,
        IReadOnlyCollection<Guid> principalKeys,
        DateTime deletedAt,
        Guid? deletedBy,
        CancellationToken cancellationToken
    )
    {
        var dependent = relation.DeclaringEntityType;
        var helper = dbContext.GetService<ISqlGenerationHelper>();
        var storeObject = StoreObjectIdentifier.Table(dependent.GetTableName()!, dependent.GetSchema());

        string Column(IProperty? property)
        {
            return helper.DelimitIdentifier(property!.GetColumnName(storeObject)!);
        }

        var primaryKeyProperty = dependent.FindPrimaryKey()!.Properties[index: 0];
        var primaryKey = Column(primaryKeyProperty);
        var table = helper.DelimitIdentifier(storeObject.Name, storeObject.Schema);
        var trackedKeys = dbContext.ChangeTracker.Entries()
            .Where(entry => dependent.IsAssignableFrom(entry.Metadata))
            .Select(entry => (Guid)entry.Property(primaryKeyProperty.Name).CurrentValue!)
            .ToArray();
        var rowFilter =
            $"{Column(relation.Properties[index: 0])} = ANY(@{PrincipalKeysParameter}) "
            + $"AND {Column(dependent.FindProperty(nameof(ISoftDeletable.DeletedAt)))} IS NULL "
            + $"AND NOT ({primaryKey} = ANY(@{TrackedKeysParameter}))";
        var setClause =
            $"{Column(dependent.FindProperty(nameof(ISoftDeletable.DeletedAt)))} = @{DeletedAtParameter}, "
            + $"{Column(dependent.FindProperty(nameof(ISoftDeletable.DeletedBy)))} = @{DeletedByParameter}";
        if (typeof(IAuditable).IsAssignableFrom(dependent.ClrType))
        {
            setClause +=
                $", {Column(dependent.FindProperty(nameof(IAuditable.UpdatedAt)))} = @{DeletedAtParameter}"
                + $", {Column(dependent.FindProperty(nameof(IAuditable.UpdatedBy)))} = @{DeletedByParameter}";
        }

        var hasOwnCascade = FindCascadeRelations(dependent).Any();
        if (!hasOwnCascade)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                Format(UpdateByRelationTemplate, table, setClause, rowFilter),
                [
                    .. CreateTimeParameters(deletedAt, deletedBy),
                    CreateKeysParameter(PrincipalKeysParameter, principalKeys),
                    CreateKeysParameter(TrackedKeysParameter, trackedKeys),
                ],
                cancellationToken);

            return [];
        }

        var keys = await dbContext.Database
            .SqlQueryRaw<Guid>(
                Format(SelectKeysTemplate, primaryKey, table, rowFilter),
                CreateKeysParameter(PrincipalKeysParameter, principalKeys),
                CreateKeysParameter(TrackedKeysParameter, trackedKeys))
            .ToListAsync(cancellationToken);
        if (keys.Count == 0)
        {
            return keys;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            Format(UpdateByKeysTemplate, table, setClause, primaryKey),
            [
                .. CreateTimeParameters(deletedAt, deletedBy),
                CreateKeysParameter(KeysParameter, keys),
            ],
            cancellationToken);

        return keys;
    }

    private static string Format(string template, params object[] values) =>
        string.Format(CultureInfo.InvariantCulture, template, values);

    private static DbParameter[] CreateTimeParameters(DateTime deletedAt, Guid? deletedBy) =>
    [
        new NpgsqlParameter(DeletedAtParameter, NpgsqlDbType.TimestampTz) { Value = deletedAt },
        new NpgsqlParameter(DeletedByParameter, NpgsqlDbType.Uuid) { Value = (object?)deletedBy ?? DBNull.Value },
    ];

    private static NpgsqlParameter CreateKeysParameter(string name, IEnumerable<Guid> keys) =>
        new(name, NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = keys.ToArray() };
}
