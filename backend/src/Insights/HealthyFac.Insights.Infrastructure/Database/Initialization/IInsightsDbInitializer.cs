namespace HealthyFac.Insights.Infrastructure.Database.Initialization;

/// <summary>
/// Defines database initialization operations.
/// </summary>
public interface IInsightsDbInitializer
{
    /// <summary>
    /// Applies database migrations to update the database schema.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ApplyDatabaseStructureAsync(CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// Seeds the demo ingest snapshot (Development only).
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SeedOptionalDataAsync(CancellationToken cancellationToken = default(CancellationToken));
}
