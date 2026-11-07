using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Application.MetricUnits;
using HealthyFac.Telemetry.Domain.MetricUnits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.MetricUnits.Persistence;

/// <summary>
/// Configures entity framework mapping for the quantity kind entity.
/// </summary>
public class QuantityKindConfiguration : IEntityTypeConfiguration<QuantityKind>
{
    public void Configure(EntityTypeBuilder<QuantityKind> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureSoftDeletable();

        builder.Property(kind => kind.Name)
            .HasMaxLength(MetricUnitValidationConstants.QuantityNameMaxLength)
            .IsRequired();

        builder.HasIndex(kind => kind.Name)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasNotBlankCheck(kind => kind.Name);
    }
}
