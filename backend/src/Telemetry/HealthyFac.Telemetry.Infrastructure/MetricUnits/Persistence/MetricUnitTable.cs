using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Infrastructure.MetricUnits.Persistence;

/// <summary>
/// Names the tables and the columns of the unit catalog for the two raw SQL statements of the catalog seed.
/// </summary>
internal static class MetricUnitTable
{
    public const string KindsTable = "quantity_kinds";
    public const string UnitsTable = "measurement_units";
    public const string IdColumn = "id";
    public const string NameColumn = "name";
    public const string QuantityKindIdColumn = "quantity_kind_id";
    public const string AbbreviationColumn = "abbreviation";

    /// <summary>
    /// Inserts the missing quantity kinds. The conflict target repeats the filter of the unique index. Parameters:
    /// the IDs, the kind names.
    /// </summary>
    public const string KindUpsertSql =
        $"INSERT INTO {KindsTable} ({IdColumn}, {NameColumn}) "
        + "SELECT * FROM unnest({0}, {1}) "
        + $"ON CONFLICT ({NameColumn}) WHERE {SoftDeleteConstants.NotDeletedFilter} DO NOTHING";

    /// <summary>
    /// Inserts the missing units and updates the abbreviation of the existing ones. A unit finds its kind by the kind
    /// name. Parameters: the IDs, the kind names, the unit names, the abbreviations.
    /// </summary>
    public const string UnitUpsertSql =
        $"INSERT INTO {UnitsTable} ({IdColumn}, {QuantityKindIdColumn}, {NameColumn}, {AbbreviationColumn}) "
        + $"SELECT seed.id, kind.{IdColumn}, seed.unit_name, seed.abbreviation "
        + "FROM unnest({0}, {1}, {2}, {3}) AS seed(id, kind_name, unit_name, abbreviation) "
        + $"JOIN {KindsTable} kind ON kind.{NameColumn} = seed.kind_name "
        + $"AND kind.{SoftDeleteConstants.NotDeletedFilter} "
        + $"ON CONFLICT ({QuantityKindIdColumn}, {NameColumn}) WHERE {SoftDeleteConstants.NotDeletedFilter} "
        + $"DO UPDATE SET {AbbreviationColumn} = EXCLUDED.{AbbreviationColumn}";
}
