namespace HealthyFac.Shared.Domain.Entities;

/// <summary>
/// Defines the names that the soft delete of an entity uses. The class lives in Domain, because Application code names
/// the filter when it must see deleted rows.
/// </summary>
public static class SoftDeleteConstants
{
    /// <summary>The index covers only rows that are not deleted, so a deleted value is free again.</summary>
    public const string NotDeletedFilter = "deleted_at IS NULL";

    /// <summary>
    /// The name of the query filter that hides deleted rows. A query that must see them ignores only this filter.
    /// </summary>
    public const string QueryFilterName = "SoftDelete";
}
