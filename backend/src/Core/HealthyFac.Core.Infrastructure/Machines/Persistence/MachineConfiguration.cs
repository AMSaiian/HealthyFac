using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;

namespace HealthyFac.Core.Infrastructure.Machines.Persistence;

/// <summary>
/// Configures entity framework mapping for the machine entity. The department reference includes the company,
/// so the database rejects a machine whose department belongs to another company.
/// </summary>
public class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(machine => machine.Name)
            .HasMaxLength(MachineValidationConstants.NameMaxLength)
            .IsRequired();

        builder.Property(machine => machine.SerialNumber)
            .HasMaxLength(MachineValidationConstants.SerialNumberMaxLength)
            .IsRequired();

        builder.Property(machine => machine.Description)
            .HasMaxLength(MachineValidationConstants.DescriptionMaxLength);

        builder.Property(machine => machine.State)
            .IsRequired();

        builder.HasIndex(machine => new { machine.CompanyId, machine.SerialNumber })
            .HasDatabaseName(MachineIndexNames.CompanySerialNumber)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(machine => machine.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(machine => new { machine.DepartmentId, machine.CompanyId })
            .HasPrincipalKey(department => new { department.Id, department.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasNotBlankCheck(machine => machine.Name)
            .HasNotBlankCheck(machine => machine.SerialNumber)
            .HasLengthCheck(machine => machine.Description, 0, MachineValidationConstants.DescriptionMaxLength)
            .HasNotEmptyGuidCheck(machine => machine.CompanyId)
            .HasNotEmptyGuidCheck(machine => machine.DepartmentId)
            .HasEnumerationCheck(machine => machine.State);
    }
}
