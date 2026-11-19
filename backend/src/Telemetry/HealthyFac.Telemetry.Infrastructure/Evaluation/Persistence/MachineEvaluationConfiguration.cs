using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Telemetry.Domain.Evaluation;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.Evaluation.Persistence;

/// <summary>
/// Configures entity framework mapping for the machine evaluation entity. The ID is the ID of the machine
/// registration. The phase tracking is a complex property, stored in the same row. The row version (<c>xmin</c>)
/// makes a second worker that took the same machine fail its save.
/// </summary>
public class MachineEvaluationConfiguration : IEntityTypeConfiguration<MachineEvaluation>
{
    public void Configure(EntityTypeBuilder<MachineEvaluation> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureVersioned();

        builder.HasOne<MachineRegistration>()
            .WithOne()
            .HasForeignKey<MachineEvaluation>(evaluation => evaluation.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(evaluation => evaluation.DueAt)
            .HasDatabaseName(EvaluationIndexNames.DueAt);

        builder.ComplexProperty(
            evaluation => evaluation.PhaseTracking,
            tracking =>
            {
                tracking.Property(value => value.CurrentPhaseId).HasColumnName(name: "current_phase_id");
                tracking.Property(value => value.SwitchedAt).HasColumnName(name: "switched_at");
                tracking.Property(value => value.MismatchSince).HasColumnName(name: "mismatch_since");
                tracking.Property(value => value.UnmatchedSince).HasColumnName(name: "unmatched_since");
            });
    }
}
