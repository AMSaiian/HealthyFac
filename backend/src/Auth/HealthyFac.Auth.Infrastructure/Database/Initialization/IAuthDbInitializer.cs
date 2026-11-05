namespace HealthyFac.Auth.Infrastructure.Database.Initialization;

/// <summary>
/// Defines the database initialization of the auth service. The service has no required or optional seed data.
/// </summary>
public interface IAuthDbInitializer
{
    /// <summary>
    /// Applies database migrations to update the database schema.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ApplyDatabaseStructureAsync(CancellationToken cancellationToken = default(CancellationToken));
}
