using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace HealthyFac.Testing.Persistence;

/// <summary>
/// Deletes the rows of every table of a context between tests, in foreign key order, and keeps the migration history
/// and the reference tables that the fixture lists. Create it once, after the migrations, because Respawn reads the
/// table graph from the database when it is created.
/// </summary>
public sealed class DatabaseResetter
{
    private const string DefaultSchema = "public";
    private const string MigrationHistoryTable = "__EFMigrationsHistory";

    private readonly string _connectionString;
    private readonly Respawner _respawner;

    private DatabaseResetter(string connectionString, Respawner respawner)
    {
        _connectionString = connectionString;
        _respawner = respawner;
    }

    /// <summary>
    /// Reads the table graph of the database. The reset covers the schemas of the tables of the model of
    /// <paramref name="context"/>.
    /// </summary>
    /// <param name="connectionString">
    /// The connection string of the test database, with its password. The connection string of the context does not
    /// work here, because the data source of a service removes the password from it.
    /// </param>
    /// <param name="context">A context of the migrated test database. Its model names the tables.</param>
    /// <param name="ignoredEntityTypes">
    /// The entities whose tables the reset keeps, for example reference data that the required seed fills and no test
    /// writes. A test that writes such a table must not list it.
    /// </param>
    /// <returns>The resetter for the database.</returns>
    public static async Task<DatabaseResetter> CreateAsync(
        string connectionString,
        DbContext context,
        params Type[] ignoredEntityTypes
    )
    {
        var schemas = context.Model.GetEntityTypes()
            .Where(entityType => entityType.GetTableName() is not null)
            .Select(entityType => entityType.GetSchema() ?? context.Model.GetDefaultSchema() ?? DefaultSchema)
            .Distinct()
            .ToArray();
        var ignoredTables = ignoredEntityTypes
            .Select(context.Model.FindEntityType)
            .Select(entityType => ToTable(entityType!, context.Model))
            .Concat(schemas.Select(schema => new Table(schema, MigrationHistoryTable)))
            .ToArray();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = schemas,
                TablesToIgnore = ignoredTables,
            });

        return new DatabaseResetter(connectionString, respawner);
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await _respawner.ResetAsync(connection);
    }

    private static Table ToTable(IEntityType entityType, IModel model)
        => new(entityType.GetSchema() ?? model.GetDefaultSchema() ?? DefaultSchema, entityType.GetTableName()!);
}
