using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;

namespace HealthyFac.Core.Infrastructure.Machines.Persistence;

/// <summary>
/// Configures entity framework mapping for the machine entity.
/// </summary>
public class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();

        builder.Property(machine => machine.Name)
            .HasMaxLength(MachineValidationConstants.NameMaxLength)
            .IsRequired();

        builder.Property(machine => machine.SerialNumber)
            .HasMaxLength(MachineValidationConstants.SerialNumberMaxLength)
            .IsRequired();

        builder.Property(machine => machine.State)
            .IsRequired();

        builder.HasNotBlankCheck(machine => machine.Name)
            .HasNotBlankCheck(machine => machine.SerialNumber)
            .HasNotEmptyGuidCheck(machine => machine.TenantId)
            .HasEnumerationCheck(machine => machine.State);

        builder.Property(machine => machine.TenantId)
            .IsRequired();
    }
}
