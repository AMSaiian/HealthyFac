using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.MetricDefinitions.Persistence;

/// <summary>
/// Configures entity framework mapping for the metric schema revision entity. The schema is JSON, so the column has
/// the type <c>jsonb</c>. The unique index on the registration and the revision number guards a parallel apply.
/// </summary>
public class MetricSchemaRevisionConfiguration : IEntityTypeConfiguration<MetricSchemaRevision>
{
    private const string SchemaColumnType = "jsonb";

    public void Configure(EntityTypeBuilder<MetricSchemaRevision> builder)
    {
        builder.ConfigureEntity();

        builder.Property(revision => revision.Schema)
            .HasColumnType(SchemaColumnType)
            .IsRequired();

        builder.HasIndex(revision => new { revision.MachineRegistrationId, revision.Revision })
            .IsUnique();

        builder.HasOne<MachineRegistration>()
            .WithMany()
            .HasForeignKey(revision => revision.MachineRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasRangeCheck(revision => revision.Revision, min: 0, int.MaxValue)
            .HasNotEmptyGuidCheck(revision => revision.MachineRegistrationId);
    }
}
