using HealthyFac.Insights.Infrastructure.Database.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.Infrastructure.Database.Initialization;

/// <summary>
/// Initializes the insights database.
/// </summary>
public class InsightsDbInitializer(
    ILogger<InsightsDbInitializer> logger,
    InsightsDbContext dbContext,
    IHostEnvironment environment,
    DemoIngestSeeder demoIngestSeeder
) : IInsightsDbInitializer
{
    private readonly ILogger<InsightsDbInitializer> _logger = logger;
    private readonly InsightsDbContext _dbContext = dbContext;
    private readonly IHostEnvironment _environment = environment;
    private readonly DemoIngestSeeder _demoIngestSeeder = demoIngestSeeder;

    /// <summary>
    /// Applies database migrations to the insights database context, only in the Development environment. Other
    /// environments apply migrations in a deployment step, so a service never changes a shared schema while it
    /// starts.
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
    /// Seeds the demo ingest snapshot, only in the Development environment. It adds nothing that exists, so a
    /// restart does not duplicate the data.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SeedOptionalDataAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_environment.IsDevelopment())
        {
            await _demoIngestSeeder.SeedAsync(cancellationToken);
        }
    }
}
