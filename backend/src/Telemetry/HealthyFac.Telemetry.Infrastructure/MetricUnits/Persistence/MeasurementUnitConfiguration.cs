using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Application.MetricUnits;
using HealthyFac.Telemetry.Domain.MetricUnits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.MetricUnits.Persistence;

/// <summary>
/// Configures entity framework mapping for the measurement unit entity.
/// </summary>
public class MeasurementUnitConfiguration : IEntityTypeConfiguration<MeasurementUnit>
{
    public void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureSoftDeletable();

        builder.Property(unit => unit.Name)
            .HasMaxLength(MetricUnitValidationConstants.UnitNameMaxLength)
            .IsRequired();

        builder.Property(unit => unit.Abbreviation)
            .HasMaxLength(MetricUnitValidationConstants.AbbreviationMaxLength)
            .IsRequired();

        builder.HasIndex(unit => new { unit.QuantityKindId, unit.Name })
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasOne(unit => unit.QuantityKind)
            .WithMany()
            .HasForeignKey(unit => unit.QuantityKindId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasNotBlankCheck(unit => unit.Name)
            .HasNotEmptyGuidCheck(unit => unit.QuantityKindId);
    }
}
