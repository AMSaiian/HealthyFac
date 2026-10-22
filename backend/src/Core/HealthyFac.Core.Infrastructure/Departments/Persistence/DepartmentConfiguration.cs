using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HealthyFac.Core.Application.Departments;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;

namespace HealthyFac.Core.Infrastructure.Departments.Persistence;

/// <summary>
/// Configures entity framework mapping for the department entity. The alternate key on the identifier and the
/// company lets rows of other tables reference a department together with its company, so the database
/// rejects a row whose department belongs to another company.
/// </summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();

        builder.Property(department => department.Name)
            .HasMaxLength(DepartmentValidationConstants.NameMaxLength)
            .IsRequired();

        builder.Property(department => department.TimeZoneId)
            .HasMaxLength(DepartmentValidationConstants.TimeZoneIdMaxLength)
            .IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(department => department.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(department => new { department.Id, department.CompanyId });

        builder.HasIndex(department => new { department.CompanyId, department.Name })
            .IsUnique();

        builder.HasNotBlankCheck(department => department.Name)
            .HasNotBlankCheck(department => department.TimeZoneId)
            .HasNotEmptyGuidCheck(department => department.CompanyId);
    }
}
