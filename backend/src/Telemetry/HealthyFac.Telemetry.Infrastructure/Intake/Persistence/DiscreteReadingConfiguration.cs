using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Persistence;

/// <summary>
/// Configures entity framework mapping for the discrete reading. The natural key is the primary key.
/// </summary>
public class DiscreteReadingConfiguration : IEntityTypeConfiguration<DiscreteReading>
{
    public void Configure(EntityTypeBuilder<DiscreteReading> builder)
    {
        builder.HasKey(reading => new { reading.MetricDefinitionId, reading.SampledAt });

        builder.Property(reading => reading.Value)
            .IsRequired();

        builder.HasOne<MetricDefinition>()
            .WithMany()
            .HasForeignKey(reading => reading.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
