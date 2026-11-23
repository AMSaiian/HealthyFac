namespace HealthyFac.Core.Infrastructure.Database.Initialization;

/// <summary>
/// Defines database initialization operations: the schema and the required data.
/// </summary>
public interface ICoreDbInitializer
{
    /// <summary>
    /// Applies database migrations to update the database schema.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ApplyDatabaseStructureAsync(CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// Seeds required core data into the database.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SeedRequiredDataAsync(CancellationToken cancellationToken = default(CancellationToken));
}
