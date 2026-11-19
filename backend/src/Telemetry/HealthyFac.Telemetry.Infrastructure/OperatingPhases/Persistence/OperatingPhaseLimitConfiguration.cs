using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.OperatingPhases.Persistence;

/// <summary>
/// Configures the table <c>operating_phase_limits</c>, the limit rows of a phase. A unique index over the phase and
/// the metric guards one active row per metric: a deleted row frees the pair. The foreign key to the metric restricts
/// a delete. The four limit columns are plain properties.
/// </summary>
public class OperatingPhaseLimitConfiguration : IEntityTypeConfiguration<OperatingPhaseLimit>
{
    public const string TableName = "operating_phase_limits";

    public void Configure(EntityTypeBuilder<OperatingPhaseLimit> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.ToTable(TableName);

        builder.HasIndex(
                OperatingPhaseConditionConfiguration.PhaseIdName, nameof(OperatingPhaseLimit.MetricDefinitionId))
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasOne<MetricDefinition>()
            .WithMany()
            .HasForeignKey(limit => limit.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
