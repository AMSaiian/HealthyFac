using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Auth.Infrastructure.Database;

/// <summary>
/// Initializes the auth database.
/// </summary>
public class AuthDbInitializer(
    ILogger<AuthDbInitializer> logger,
    AuthDbContext dbContext,
    IHostEnvironment environment
) : IAuthDbInitializer
{
    private readonly ILogger<AuthDbInitializer> _logger = logger;
    private readonly AuthDbContext _dbContext = dbContext;
    private readonly IHostEnvironment _environment = environment;

    /// <summary>
    /// Applies database migrations to the auth database context, only in the Development
    /// environment. Other environments apply migrations in a deployment step, so a service never changes
    /// a shared schema while it starts.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ApplyDatabaseStructureAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            _logger.LogInformation(DatabaseConstants.Messages.MigrationSkipped, _environment.EnvironmentName);

            return;
        }

        try
        {
            if (_dbContext.Database.IsRelational())
            {
                _logger.LogInformation(DatabaseConstants.Messages.MigrationApplying);
                await _dbContext.Database.MigrateAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, DatabaseConstants.Errors.Migration);
            throw;
        }
    }
}
