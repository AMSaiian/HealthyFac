namespace HealthyFac.Shared.Infrastructure.Persistence;

/// <summary>
/// Defines database-level defaults applied by the shared entity configuration.
/// </summary>
public static class PersistenceConstants
{
    /// <summary>
    /// Native PostgreSQL 18 identifier generator. It produces time-ordered UUID version 7 values,
    /// which keep the primary key index append-only.
    /// </summary>
    public const string IdentifierSql = "uuidv7()";
}
