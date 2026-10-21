namespace HealthyFac.Shared.Infrastructure.Messaging.Persistence;

/// <summary>
/// Defines the column and index settings that the outbox and inbox tables share.
/// </summary>
internal static class MessagingPersistenceConstants
{
    public const int TypeMaxLength = 200;
    public const string PayloadColumnType = "jsonb";

    /// <summary>
    /// The PostgreSQL system column that changes on every row update. A <see langword="uint"/> row version
    /// property with this name maps to it, so two processors that save the same row cannot both succeed. The
    /// name is already snake case, so the naming convention keeps it.
    /// </summary>
    public const string ConcurrencyToken = "xmin";

    /// <summary>
    /// Limits an index to the rows that still wait for a processor. The column name is the snake case form of
    /// <c>ProcessedOn</c>. Every service context applies snake case naming after it maps the messaging tables.
    /// </summary>
    public const string PendingFilter = "processed_on IS NULL";
}
