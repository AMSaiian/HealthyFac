using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Persistence;

/// <summary>
/// Configures entity framework mapping for the numeric reading. The natural key is the primary key, so the table
/// has no <c>id</c> column and no second index.
/// </summary>
public class NumericReadingConfiguration : IEntityTypeConfiguration<NumericReading>
{
    public void Configure(EntityTypeBuilder<NumericReading> builder)
    {
        builder.HasKey(reading => new { reading.MetricDefinitionId, reading.SampledAt });

        builder.HasOne<MetricDefinition>()
            .WithMany()
            .HasForeignKey(reading => reading.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
