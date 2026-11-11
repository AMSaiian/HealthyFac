using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.Employees.Persistence;

/// <summary>
/// Configures entity framework mapping for the employee entity. The email is unique across all companies,
/// because Keycloak rejects a second user with the same email in one realm.
/// </summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(employee => employee.Email)
            .HasMaxLength(EmployeeValidationConstants.EmailMaxLength)
            .IsRequired();

        builder.Property(employee => employee.FirstName)
            .HasMaxLength(EmployeeValidationConstants.NameMaxLength)
            .IsRequired();

        builder.Property(employee => employee.LastName)
            .HasMaxLength(EmployeeValidationConstants.NameMaxLength)
            .IsRequired();

        builder.Property(employee => employee.Status)
            .IsRequired();

        builder.ConfigureVersioned();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(employee => employee.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(employee => employee.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(employee => employee.Email)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasIndex(employee => employee.IdentityUserId)
            .IsUnique()
            .HasFilter($"identity_user_id IS NOT NULL AND {SoftDeleteConstants.NotDeletedFilter}");

        builder.HasNotBlankCheck(employee => employee.Email)
            .HasNotBlankCheck(employee => employee.FirstName)
            .HasNotBlankCheck(employee => employee.LastName)
            .HasNotEmptyGuidCheck(employee => employee.CompanyId)
            .HasNotEmptyGuidCheck(employee => employee.RoleId)
            .HasEnumerationCheck(employee => employee.Status);
    }
}
