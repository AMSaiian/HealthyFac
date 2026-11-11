using HealthyFac.Shared.Domain.Gateways;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.MachineRegistrations.Persistence;

/// <summary>
/// Configures entity framework mapping for the machine registration entity. The unique index on the gateway ID keeps
/// deleted rows, because a gateway ID is never reused. The row version (<c>xmin</c>) makes two parallel applies of
/// metric definition changes conflict.
/// </summary>
public class MachineRegistrationConfiguration : IEntityTypeConfiguration<MachineRegistration>
{
    public void Configure(EntityTypeBuilder<MachineRegistration> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureSoftDeletable();

        builder.Property(registration => registration.GatewayId)
            .HasMaxLength(GatewayIds.Length)
            .IsRequired();

        builder.ConfigureVersioned();

        builder.HasIndex(registration => registration.GatewayId)
            .HasDatabaseName(MachineRegistrationIndexNames.GatewayId)
            .IsUnique();

        builder.HasNotEmptyGuidCheck(registration => registration.CompanyId)
            .HasLengthCheck(registration => registration.GatewayId, GatewayIds.Length, GatewayIds.Length);
    }
}
