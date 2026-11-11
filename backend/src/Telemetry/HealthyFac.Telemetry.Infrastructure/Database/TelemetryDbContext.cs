using System.Data;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.Infrastructure.Messaging.Sending;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.Enumerations;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricUnits;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Infrastructure.Database;

/// <summary>
/// Database context for the telemetry service. Use cases reach it through <see cref="ITelemetryDbContext"/>, so they
/// need no reference to this layer. The outbox and inbox reach it through their own interfaces, and
/// <see cref="MessagingExtensions"/> maps every interface to one scoped instance.
/// </summary>
/// <param name="options">The options for this context.</param>
public class TelemetryDbContext(DbContextOptions<TelemetryDbContext> options)
    : DbContext(options), ITelemetryDbContext, IOutboxDbContext, IInboxDbContext
{
    public DbSet<MachineRegistration> MachineRegistrations => Set<MachineRegistration>();

    public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();

    public DbSet<MetricSchemaRevision> MetricSchemaRevisions => Set<MetricSchemaRevision>();

    public DbSet<StatusDefinition> StatusDefinitions => Set<StatusDefinition>();

    public DbSet<ReceivedMessage> ReceivedMessages => Set<ReceivedMessage>();

    public DbSet<NumericReading> NumericReadings => Set<NumericReading>();

    public DbSet<DiscreteReading> DiscreteReadings => Set<DiscreteReading>();

    public DbSet<StatusReading> StatusReadings => Set<StatusReading>();

    public DbSet<QuantityKind> QuantityKinds => Set<QuantityKind>();

    public DbSet<MeasurementUnit> MeasurementUnits => Set<MeasurementUnit>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public Task RunInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => RunInTransactionAsync(operation, IsolationLevel.ReadCommitted, cancellationToken);

    public Task<TResult> RunInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => RunInTransactionAsync(operation, IsolationLevel.ReadCommitted, cancellationToken);

    public Task RunInTransactionAsync(
        Func<CancellationToken, Task> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => RunInTransactionAsync(
            async token =>
            {
                await operation(token);

                return true;
            },
            isolationLevel,
            cancellationToken);

    public async Task<TResult> RunInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (ChangeTracker.HasChanges())
        {
            throw PersistenceErrors.UnsavedChangesBeforeTransaction();
        }

        return await Database
            .CreateExecutionStrategy()
            .ExecuteAsync(
                token => RunTransactionAttemptAsync(operation, isolationLevel, token),
                cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.HaveEnumerationConversion<StatusSeverity>();
        configurationBuilder.HaveEnumerationConversion<MetricValueKind>();
        configurationBuilder.IgnoreAny<IntakeError>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(TelemetryInfrastructureAssembly.Instance);
        modelBuilder.ApplyOutboxConfiguration();
        modelBuilder.ApplyInboxConfiguration();
        modelBuilder.UseCaseInsensitiveText();
        modelBuilder.ToSnakeCaseConventions();
        modelBuilder.HasPostgreSqlExtensions(DatabaseConstants.RequiredPostgresExtensions);
    }

    private async Task<TResult> RunTransactionAttemptAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken
    )
    {
        ChangeTracker.Clear();
        await using var transaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);

        var result = await operation(cancellationToken);
        if (Database.CurrentTransaction != transaction)
        {
            throw PersistenceErrors.TransactionEndedByOperation();
        }

        if (ChangeTracker.HasChanges())
        {
            throw PersistenceErrors.UnsavedChangesAfterOperation();
        }

        await transaction.CommitAsync(cancellationToken);

        return result;
    }
}
