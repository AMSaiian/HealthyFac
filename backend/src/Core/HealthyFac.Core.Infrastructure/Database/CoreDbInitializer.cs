using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Core.Infrastructure.Database;

/// <summary>
/// Initializes the core database and performs data seeding operations.
/// </summary>
public class CoreDbInitializer(
    ILogger<CoreDbInitializer> logger,
    CoreDbContext dbContext,
    IHostEnvironment environment
) : ICoreDbInitializer
{
    /// <summary>
    /// Applies database migrations to the core database context, only in the Development
    /// environment. Other environments apply migrations in a deployment step, so a service never changes
    /// a shared schema while it starts.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ApplyDatabaseStructureAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            logger.LogInformation(DatabaseConstants.Messages.MigrationSkipped, environment.EnvironmentName);

            return;
        }

        try
        {
            if (dbContext.Database.IsRelational())
            {
                logger.LogInformation(DatabaseConstants.Messages.MigrationApplying);
                await dbContext.Database.MigrateAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, DatabaseConstants.Errors.Migration);
            throw;
        }
    }

    /// <summary>
    /// Seeds required core data into the core database context.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SeedRequiredDataAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// Seeds optional development and test data into the core database context.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SeedOptionalDataAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
