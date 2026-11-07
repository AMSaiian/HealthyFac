using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.MetricDefinitions.Persistence;

/// <summary>
/// Configures entity framework mapping for the metric definition entity. The unique index on the registration and
/// the key is the guard for the uniqueness of a key: a deleted definition frees its key.
/// </summary>
public class MetricDefinitionConfiguration : IEntityTypeConfiguration<MetricDefinition>
{
    public void Configure(EntityTypeBuilder<MetricDefinition> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(metric => metric.Key)
            .HasMaxLength(MetricDefinitionRules.KeyMaxLength)
            .IsRequired();

        builder.Property(metric => metric.Name)
            .HasMaxLength(MetricDefinitionValidationConstants.NameMaxLength)
            .IsRequired();

        builder.Property(metric => metric.ValueKind)
            .IsRequired();

        builder.Property(metric => metric.States)
            .HasField(fieldName: "_states")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(metric => new { metric.MachineRegistrationId, metric.Key })
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasOne<MachineRegistration>()
            .WithMany()
            .HasForeignKey(metric => metric.MachineRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(metric => metric.MeasurementUnit)
            .WithMany()
            .HasForeignKey(metric => metric.MeasurementUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasEnumerationCheck(metric => metric.ValueKind)
            .HasNotBlankCheck(metric => metric.Key)
            .HasNotBlankCheck(metric => metric.Name)
            .HasLengthCheck(metric => metric.Key, minLength: 1, MetricDefinitionRules.KeyMaxLength)
            .HasLengthCheck(metric => metric.Description, minLength: 0, RichTextConstants.HtmlMaxLength)
            .HasNotEmptyGuidCheck(metric => metric.MachineRegistrationId);
    }
}
