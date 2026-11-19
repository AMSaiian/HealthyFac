using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Telemetry.Domain.Evaluation.Statistics;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.Evaluation.Persistence;

/// <summary>
/// Configures entity framework mapping for the metric statistics entity. A null phase means the phase none, and the
/// unique index treats two nulls as equal (<c>AreNullsDistinct(false)</c>), so a metric has one row for the phase
/// none. A removal of the metric or the phase is restricted: the evaluation deletes the statistics rows itself.
/// </summary>
public class MetricStatisticsConfiguration : IEntityTypeConfiguration<MetricStatistics>
{
    public void Configure(EntityTypeBuilder<MetricStatistics> builder)
    {
        builder.ConfigureEntity();

        builder.HasOne<MetricDefinition>()
            .WithMany()
            .HasForeignKey(statistics => statistics.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OperatingPhase>()
            .WithMany()
            .HasForeignKey(statistics => statistics.OperatingPhaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(statistics => new { statistics.MetricDefinitionId, statistics.OperatingPhaseId })
            .HasDatabaseName(EvaluationIndexNames.MetricPhase)
            .IsUnique()
            .AreNullsDistinct(nullsDistinct: false);
    }
}
