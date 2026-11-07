using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Domain.Gateways;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

        builder.Property(machine => machine.GatewayId)
            .HasMaxLength(GatewayIds.Length)
            .IsRequired();

        builder.HasIndex(machine => machine.GatewayId)
            .HasDatabaseName(MachineIndexNames.GatewayId)
            .IsUnique();

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
            .HasLengthCheck(machine => machine.GatewayId, GatewayIds.Length, GatewayIds.Length)
            .HasLengthCheck(machine => machine.Description, minLength: 0, RichTextConstants.HtmlMaxLength)
            .HasNotEmptyGuidCheck(machine => machine.CompanyId)
            .HasNotEmptyGuidCheck(machine => machine.DepartmentId);
    }
}
