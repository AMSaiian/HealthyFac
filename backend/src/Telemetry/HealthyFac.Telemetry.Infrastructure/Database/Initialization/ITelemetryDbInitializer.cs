namespace HealthyFac.Telemetry.Infrastructure.Database.Initialization;

/// <summary>
/// Defines database initialization and data seeding operations.
/// </summary>
public interface ITelemetryDbInitializer
{
    /// <summary>
    /// Applies database migrations to update the database schema.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ApplyDatabaseStructureAsync(CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// Seeds required telemetry data into the database.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SeedRequiredDataAsync(CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// Seeds optional development and test data into the database.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SeedOptionalDataAsync(CancellationToken cancellationToken = default(CancellationToken));
}
