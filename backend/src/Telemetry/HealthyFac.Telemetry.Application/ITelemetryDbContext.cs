using HealthyFac.Shared.Application.Persistence;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
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
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HealthyFac.Telemetry.Application;

/// <summary>
/// Declares the sets and the <see cref="DbContext"/> members that use cases need. The Infrastructure layer
/// implements it, so the Application layer needs no reference to Infrastructure. Declare every new
/// <see cref="DbSet{TEntity}"/> here. Copy a <see cref="DbContext"/> member signature only when a use case calls it.
/// </summary>
public interface ITelemetryDbContext : ITransactionalDbContext
{
    DbSet<MachineRegistration> MachineRegistrations { get; }

    DbSet<OperatingPhase> OperatingPhases { get; }

    DbSet<MachineEvaluation> MachineEvaluations { get; }

    DbSet<MetricStatistics> MetricStatistics { get; }

    DbSet<RuleStreak> RuleStreaks { get; }

    DbSet<PhaseInterval> PhaseIntervals { get; }

    DbSet<SignalEpisode> SignalEpisodes { get; }

    DbSet<StatusDefinition> StatusDefinitions { get; }

    DbSet<MetricDefinition> MetricDefinitions { get; }

    DbSet<MetricSchemaRevision> MetricSchemaRevisions { get; }

    DbSet<ProposalEntry> ProposalEntries { get; }

    DbSet<MetricDefinitionDraft> MetricDefinitionDrafts { get; }

    DbSet<StatusDefinitionDraft> StatusDefinitionDrafts { get; }

    DbSet<OperatingPhaseDraft> OperatingPhaseDrafts { get; }

    DbSet<ReceivedMessage> ReceivedMessages { get; }

    DbSet<NumericReading> NumericReadings { get; }

    DbSet<DiscreteReading> DiscreteReadings { get; }

    DbSet<StatusReading> StatusReadings { get; }

    DbSet<QuantityKind> QuantityKinds { get; }

    DbSet<MeasurementUnit> MeasurementUnits { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}
