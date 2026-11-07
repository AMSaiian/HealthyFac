using HealthyFac.Telemetry.Infrastructure.Database.Seeding;
using HealthyFac.Telemetry.Infrastructure.MetricUnits.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Telemetry.Infrastructure.Database.Initialization;

/// <summary>
/// Initializes the telemetry database and performs data seeding operations.
/// </summary>
public class TelemetryDbInitializer(
    ILogger<TelemetryDbInitializer> logger,
    TelemetryDbContext dbContext,
    IHostEnvironment environment,
    TelemetryDevelopmentDataSeeder developmentDataSeeder
) : ITelemetryDbInitializer
{
    private readonly ILogger<TelemetryDbInitializer> _logger = logger;
    private readonly TelemetryDbContext _dbContext = dbContext;
    private readonly IHostEnvironment _environment = environment;
    private readonly TelemetryDevelopmentDataSeeder _developmentDataSeeder = developmentDataSeeder;

    /// <summary>
    /// Applies database migrations to the telemetry database context, only in the Development
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
    /// Seeds the unit catalog with two statements that each insert the missing rows and keep the existing ones. The
    /// conflict targets repeat the filter of the unique indexes (<c>deleted_at IS NULL</c>), because PostgreSQL
    /// otherwise finds no matching index. A parallel run waits on a row of the first statement and then finds it, so
    /// the seed is safe to run on every start, also when several instances start at the same time.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SeedRequiredDataAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        var rows = MetricUnitSeed.Rows;
        var kindNames = rows.Select(row => row.QuantityName).Distinct().ToArray();

        await _dbContext.Database.ExecuteSqlRawAsync(
            MetricUnitTable.KindUpsertSql,
            [kindNames.Select(_ => Guid.CreateVersion7()).ToArray(), kindNames],
            cancellationToken);

        await _dbContext.Database.ExecuteSqlRawAsync(
            MetricUnitTable.UnitUpsertSql,
            [
                rows.Select(_ => Guid.CreateVersion7()).ToArray(),
                rows.Select(row => row.QuantityName).ToArray(),
                rows.Select(row => row.UnitName).ToArray(),
                rows.Select(row => row.Abbreviation).ToArray(),
            ],
            cancellationToken);
    }

    /// <summary>
    /// Seeds the registrations of the seed machines with their status definitions. It adds nothing for a machine
    /// whose registration exists, so a restart does not duplicate the data.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SeedOptionalDataAsync(CancellationToken cancellationToken = default(CancellationToken))
        => _developmentDataSeeder.SeedAsync(cancellationToken);
}
