namespace HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;

/// <summary>
/// Defines the filter that unique indexes of soft-deletable entities use to skip deleted rows.
/// </summary>
public static class SoftDeleteConstants
{
    /// <summary>The index covers only rows that are not deleted, so a deleted value is free again.</summary>
    public const string NotDeletedFilter = "deleted_at IS NULL";
}
