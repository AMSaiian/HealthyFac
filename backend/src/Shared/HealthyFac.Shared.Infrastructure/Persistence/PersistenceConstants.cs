namespace HealthyFac.Shared.Infrastructure.Persistence;

/// <summary>
/// Defines PostgreSQL names that the shared entity configurations use.
/// </summary>
public static class PersistenceConstants
{
    /// <summary>
    /// The PostgreSQL system column that changes on every row update. Every table has it, so a row version
    /// property that maps to it adds no column to the schema.
    /// </summary>
    public const string RowVersionColumn = "xmin";
}
