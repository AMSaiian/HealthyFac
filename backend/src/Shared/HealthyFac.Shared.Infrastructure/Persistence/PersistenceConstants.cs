namespace HealthyFac.Shared.Infrastructure.Persistence;

/// <summary>
/// Defines PostgreSQL names that the shared entity configurations use.
/// </summary>
public static class PersistenceConstants
{
    /// <summary>
    /// The PostgreSQL system column that changes on every row update. A <see langword="uint"/> row version
    /// property with this name maps to it, so two writers that save the same row cannot both succeed. The
    /// name is already snake case, so the naming convention keeps it.
    /// </summary>
    public const string ConcurrencyToken = "xmin";
}
