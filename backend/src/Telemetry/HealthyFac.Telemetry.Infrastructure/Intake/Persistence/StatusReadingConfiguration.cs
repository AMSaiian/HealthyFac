using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Persistence;

/// <summary>
/// Configures entity framework mapping for the status reading. The natural key is the primary key.
/// </summary>
public class StatusReadingConfiguration : IEntityTypeConfiguration<StatusReading>
{
    public void Configure(EntityTypeBuilder<StatusReading> builder)
    {
        builder.HasKey(reading => new { reading.StatusDefinitionId, reading.SampledAt });

        builder.HasOne<StatusDefinition>()
            .WithMany()
            .HasForeignKey(reading => reading.StatusDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
