using HealthyFac.Core.Infrastructure.Database.Seeding;
using HealthyFac.Core.Infrastructure.Roles.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Core.Infrastructure.Database.Initialization;

/// <summary>
/// Initializes the core database and performs data seeding operations.
/// </summary>
public class CoreDbInitializer(
    ILogger<CoreDbInitializer> logger,
    CoreDbContext dbContext,
    IHostEnvironment environment,
    CoreDevelopmentDataSeeder developmentDataSeeder
) : ICoreDbInitializer
{
    private readonly ILogger<CoreDbInitializer> _logger = logger;
    private readonly CoreDbContext _dbContext = dbContext;
    private readonly IHostEnvironment _environment = environment;
    private readonly CoreDevelopmentDataSeeder _developmentDataSeeder = developmentDataSeeder;

    /// <summary>
    /// Applies database migrations to the core database context, only in the Development
    /// environment. Other environments apply migrations in a deployment step, so a service never changes
    /// a shared schema while it starts.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ApplyDatabaseStructureAsync(CancellationToken cancellationToken = default(CancellationToken))
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

    /// <summary>
    /// Seeds the global roles into the core database context with one atomic <c>INSERT ... ON CONFLICT</c>
    /// statement on the role code. The conflict target repeats the filter of the unique index
    /// (<c>deleted_at IS NULL</c>), because PostgreSQL otherwise finds no matching index. The statement inserts
    /// missing roles and updates the names of existing ones. A soft-deleted role does not block it. It is safe
    /// to run on every start, also when several instances start at the same time.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SeedRequiredDataAsync(CancellationToken cancellationToken = default(CancellationToken))
        => _dbContext.Database.ExecuteSqlRawAsync(
            RoleTable.UpsertSql,
            [
                RoleSeed.Roles.Select(_ => Guid.CreateVersion7()).ToArray(),
                RoleSeed.Roles.Select(role => role.Code.Value).ToArray(),
                RoleSeed.Roles.Select(role => role.Name).ToArray(),
            ],
            cancellationToken
        );

    /// <summary>
    /// Seeds generated development data into the core database context. It adds nothing when a company already
    /// exists, so a restart does not duplicate the data.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SeedOptionalDataAsync(CancellationToken cancellationToken = default(CancellationToken))
        => _developmentDataSeeder.SeedAsync(cancellationToken);
}
