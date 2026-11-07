using HealthyFac.Shared.Application.Persistence;
using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricUnits;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application;

/// <summary>
/// Declares the sets and the <see cref="DbContext"/> members that use cases need. The Infrastructure layer
/// implements it, so the Application layer needs no reference to Infrastructure. Declare every new
/// <see cref="DbSet{TEntity}"/> here. Copy a <see cref="DbContext"/> member signature only when a use case calls it.
/// </summary>
public interface ITelemetryDbContext : ITransactionalDbContext
{
    DbSet<MachineRegistration> MachineRegistrations { get; }

    DbSet<StatusDefinition> StatusDefinitions { get; }

    DbSet<MetricDefinition> MetricDefinitions { get; }

    DbSet<MetricSchemaRevision> MetricSchemaRevisions { get; }

    DbSet<ReceivedMessage> ReceivedMessages { get; }

    DbSet<NumericReading> NumericReadings { get; }

    DbSet<DiscreteReading> DiscreteReadings { get; }

    DbSet<StatusReading> StatusReadings { get; }

    DbSet<QuantityKind> QuantityKinds { get; }

    DbSet<MeasurementUnit> MeasurementUnits { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}
