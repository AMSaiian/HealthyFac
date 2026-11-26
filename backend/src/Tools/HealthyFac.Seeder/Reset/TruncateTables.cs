using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Seeder.Reset;

/// <summary>
/// Builds the <c>TRUNCATE</c> statement of one database from its EF model. The list holds every table of an entity
/// (also the outbox and the inbox) except the tables of the keep list. The migration history table is no entity, so it
/// stays. The statement has no <c>CASCADE</c>: a table that a kept table or a table outside the list references fails
/// the statement, which protects the data that the reset must keep.
/// </summary>
public static class TruncateTables
{
    /// <summary>Lists the quoted table names that the reset empties.</summary>
    /// <param name="model">The EF model of the database.</param>
    /// <param name="keep">The entity types whose tables stay.</param>
    /// <returns>The quoted table names, with the schema when the table has one, in the order of the names.</returns>
    public static IReadOnlyList<string> List(IModel model, IReadOnlySet<Type> keep)
    {
        var entities = model.GetEntityTypes().Where(entity => entity.GetTableName() is not null).ToList();
        var kept = entities.Where(entity => keep.Contains(entity.ClrType)).Select(Quote).ToHashSet();

        return [.. entities.Select(Quote).Where(table => !kept.Contains(table)).Distinct().Order()];
    }

    /// <summary>Builds the statement.</summary>
    /// <param name="tables">The quoted table names of <see cref="List"/>.</param>
    /// <returns>One <c>TRUNCATE</c> statement with <c>RESTART IDENTITY</c>.</returns>
    public static string BuildSql(IReadOnlyList<string> tables) =>
        $"TRUNCATE TABLE {string.Join(separator: ", ", tables)} RESTART IDENTITY";

    private static string Quote(IEntityType entity) =>
        entity.GetSchema() is { } schema
            ? $"\"{schema}\".\"{entity.GetTableName()}\""
            : $"\"{entity.GetTableName()}\"";
}
