using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;

namespace HealthyFac.Core.Infrastructure.Roles.Persistence;

/// <summary>
/// Names the table and the columns of the roles for the one raw SQL statement of the roles seed.
/// </summary>
internal static class RoleTable
{
    public const string Name = "roles";
    public const string IdColumn = "id";
    public const string CodeColumn = "code";
    public const string NameColumn = "name";

    public const string UpsertSql =
        $"INSERT INTO {Name} ({IdColumn}, {CodeColumn}, {NameColumn}) "
        + "SELECT * FROM unnest({0}, {1}, {2}) "
        + $"ON CONFLICT ({CodeColumn}) WHERE {SoftDeleteConstants.NotDeletedFilter} "
        + $"DO UPDATE SET {NameColumn} = EXCLUDED.{NameColumn}";
}
