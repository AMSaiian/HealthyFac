using System.Data;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.Infrastructure.Messaging.Sending;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.Enumerations;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.StatusDrafts;
using HealthyFac.Telemetry.Domain.Evaluation;
using HealthyFac.Telemetry.Domain.Evaluation.Phases;
using HealthyFac.Telemetry.Domain.Evaluation.Rules;
using HealthyFac.Telemetry.Domain.Evaluation.Statistics;
using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricUnits;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
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

    public DbSet<OperatingPhase> OperatingPhases => Set<OperatingPhase>();

    public DbSet<MachineEvaluation> MachineEvaluations => Set<MachineEvaluation>();

    public DbSet<MetricStatistics> MetricStatistics => Set<MetricStatistics>();

    public DbSet<RuleStreak> RuleStreaks => Set<RuleStreak>();

    public DbSet<PhaseInterval> PhaseIntervals => Set<PhaseInterval>();

    public DbSet<SignalEpisode> SignalEpisodes => Set<SignalEpisode>();

    public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();

    public DbSet<MetricSchemaRevision> MetricSchemaRevisions => Set<MetricSchemaRevision>();

    public DbSet<StatusDefinition> StatusDefinitions => Set<StatusDefinition>();

    public DbSet<ProposalEntry> ProposalEntries => Set<ProposalEntry>();

    public DbSet<DraftProposal> DraftProposals => Set<DraftProposal>();

    public DbSet<MetricDefinitionDraft> MetricDefinitionDrafts => Set<MetricDefinitionDraft>();

    public DbSet<StatusDefinitionDraft> StatusDefinitionDrafts => Set<StatusDefinitionDraft>();

    public DbSet<OperatingPhaseDraft> OperatingPhaseDrafts => Set<OperatingPhaseDraft>();

    public DbSet<ReceivedMessage> ReceivedMessages => Set<ReceivedMessage>();

    public DbSet<NumericReading> NumericReadings => Set<NumericReading>();

    public DbSet<DiscreteReading> DiscreteReadings => Set<DiscreteReading>();

    public DbSet<StatusReading> StatusReadings => Set<StatusReading>();

    public DbSet<QuantityKind> QuantityKinds => Set<QuantityKind>();

    public DbSet<MeasurementUnit> MeasurementUnits => Set<MeasurementUnit>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => TransactionRunner.SaveAsync(this, base.SaveChangesAsync, acceptAllChangesOnSuccess, cancellationToken);

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        TransactionRunner.Save(this, base.SaveChanges, acceptAllChangesOnSuccess);

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

    public Task<TResult> RunInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => TransactionRunner.RunAsync(this, operation, isolationLevel, cancellationToken);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.HaveEnumerationConversion<MachineRegistrationState>();
        configurationBuilder.HaveEnumerationConversion<MetricBehavior>();
        configurationBuilder.HaveEnumerationConversion<PhaseConditionOperator>();
        configurationBuilder.HaveEnumerationConversion<SignalRule>();
        configurationBuilder.HaveEnumerationConversion<SignalSeverity>();
        configurationBuilder.HaveEnumerationConversion<SignalEpisodeState>();
        configurationBuilder.ComplexProperties<MetricLimits>();
        configurationBuilder.ComplexProperties<SignalSnapshot>();
        configurationBuilder.ComplexProperties<PhaseTracking>();
        configurationBuilder.HaveEnumerationConversion<StatusSeverity>();
        configurationBuilder.HaveEnumerationConversion<MetricValueKind>();
        configurationBuilder.HaveEnumerationConversion<DefinitionDraftState>();
        configurationBuilder.HaveEnumerationConversion<ProposalEntryState>();
        configurationBuilder.HaveEnumerationConversion<DraftSource>();
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
}
