using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Telemetry.Domain.Evaluation.Phases;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.Evaluation.Persistence;

/// <summary>
/// Configures entity framework mapping for the phase interval entity. The index on the machine and the start serves the
/// range reads. A partial unique index allows one open interval for each machine. The end is never before the start.
/// </summary>
public class PhaseIntervalConfiguration : IEntityTypeConfiguration<PhaseInterval>
{
    public void Configure(EntityTypeBuilder<PhaseInterval> builder)
    {
        builder.ConfigureEntity();

        builder.HasOne<MachineRegistration>()
            .WithMany()
            .HasForeignKey(interval => interval.MachineRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OperatingPhase>()
            .WithMany()
            .HasForeignKey(interval => interval.OperatingPhaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(interval => new { interval.MachineRegistrationId, interval.StartedAt })
            .HasDatabaseName(EvaluationIndexNames.PhaseIntervalMachineStart);

        builder.HasIndex(interval => interval.MachineRegistrationId)
            .HasDatabaseName(EvaluationIndexNames.PhaseIntervalOpen)
            .IsUnique()
            .HasFilter(PhaseIntervalConstants.OpenFilter);

        builder.ToTable(table => table.HasCheckConstraint(
            PhaseIntervalConstants.EndNotBeforeStartConstraintName,
            PhaseIntervalConstants.EndNotBeforeStartCheck));
    }
}
