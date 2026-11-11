using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Document.Domain.MachineRegistrations;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Document.Infrastructure.MachineRegistrations.Persistence;

/// <summary>
/// Configures entity framework mapping for the machine registration entity.
/// </summary>
public class MachineRegistrationConfiguration : IEntityTypeConfiguration<MachineRegistration>
{
    public void Configure(EntityTypeBuilder<MachineRegistration> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureSoftDeletable();

        builder.Property(registration => registration.Name)
            .HasMaxLength(MachineMessageLimits.NameMaxLength)
            .IsRequired();

        builder.HasNotEmptyGuidCheck(registration => registration.CompanyId)
            .HasNotBlankCheck(registration => registration.Name)
            .HasLengthCheck(registration => registration.Name, minLength: 1, MachineMessageLimits.NameMaxLength);
    }
}
