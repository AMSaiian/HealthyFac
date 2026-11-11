using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Telemetry.Infrastructure.Database.Seeding;
using HealthyFac.Telemetry.Infrastructure.MetricUnits.Persistence;
using HealthyFac.Telemetry.Messaging.MetricUnits;
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
    TelemetryDevelopmentDataSeeder developmentDataSeeder,
    IOutbox outbox,
    TimeProvider timeProvider
) : ITelemetryDbInitializer
{
    private readonly ILogger<TelemetryDbInitializer> _logger = logger;
    private readonly TelemetryDbContext _dbContext = dbContext;
    private readonly IHostEnvironment _environment = environment;
    private readonly TelemetryDevelopmentDataSeeder _developmentDataSeeder = developmentDataSeeder;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

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
    /// the seed is safe to run on every start, also when several instances start at the same time. Both statements
    /// and the outbox row run in one transaction. The outbox row (<see cref="MetricUnitsChanged"/>) exists only when a
    /// kind was inserted, a unit was inserted, or an abbreviation changed, so a start without a change tells nobody.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SeedRequiredDataAsync(CancellationToken cancellationToken = default(CancellationToken))
        => _dbContext.RunInTransactionAsync(SeedCatalogAsync, cancellationToken);

    /// <summary>
    /// Seeds the registrations of the seed machines with their status definitions. It adds nothing for a machine
    /// whose registration exists, so a restart does not duplicate the data.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SeedOptionalDataAsync(CancellationToken cancellationToken = default(CancellationToken))
        => _developmentDataSeeder.SeedAsync(cancellationToken);

    private async Task SeedCatalogAsync(CancellationToken cancellationToken)
    {
        var rows = MetricUnitSeed.Rows;
        var kindNames = rows.Select(row => row.QuantityName).Distinct().ToArray();

        var insertedKinds = await _dbContext.Database.ExecuteSqlRawAsync(
            MetricUnitTable.KindUpsertSql,
            [kindNames.Select(_ => Guid.CreateVersion7()).ToArray(), kindNames],
            cancellationToken);

        var changedUnits = await _dbContext.Database.ExecuteSqlRawAsync(
            MetricUnitTable.UnitUpsertSql,
            [
                rows.Select(_ => Guid.CreateVersion7()).ToArray(),
                rows.Select(row => row.QuantityName).ToArray(),
                rows.Select(row => row.UnitName).ToArray(),
                rows.Select(row => row.Abbreviation).ToArray(),
            ],
            cancellationToken);

        if (insertedKinds + changedUnits > 0)
        {
            _outbox.Add(new MetricUnitsChanged { ChangedAt = _timeProvider.GetUtcNow().UtcDateTime });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
