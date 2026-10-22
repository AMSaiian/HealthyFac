using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;

namespace HealthyFac.Core.Infrastructure.Employees.Persistence;

/// <summary>
/// Configures entity framework mapping for the employee entity. The email is unique across all companies,
/// because Keycloak rejects a second user with the same email in one realm. The department links carry the
/// company and reference both sides together with it, so the database rejects a link to a department of
/// another company.
/// </summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();

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

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(employee => employee.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(employee => employee.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(employee => new { employee.Id, employee.CompanyId });

        builder.HasMany(employee => employee.Departments)
            .WithMany()
            .UsingEntity(
                "employee_departments",
                department => department.HasOne(typeof(Department))
                    .WithMany()
                    .HasForeignKey(nameof(Department) + nameof(Department.Id), nameof(Department.CompanyId))
                    .HasPrincipalKey(nameof(Department.Id), nameof(Department.CompanyId))
                    .OnDelete(DeleteBehavior.Restrict),
                employee => employee.HasOne(typeof(Employee))
                    .WithMany()
                    .HasForeignKey(nameof(Employee) + nameof(Employee.Id), nameof(Employee.CompanyId))
                    .HasPrincipalKey(nameof(Employee.Id), nameof(Employee.CompanyId))
                    .OnDelete(DeleteBehavior.Cascade),
                join => join.HasKey(
                    nameof(Employee) + nameof(Employee.Id), nameof(Department) + nameof(Department.Id)));

        builder.HasIndex(employee => employee.Email)
            .IsUnique();

        builder.HasIndex(employee => employee.IdentityUserId)
            .IsUnique()
            .HasFilter("identity_user_id IS NOT NULL");

        builder.HasNotBlankCheck(employee => employee.Email)
            .HasNotBlankCheck(employee => employee.FirstName)
            .HasNotBlankCheck(employee => employee.LastName)
            .HasNotEmptyGuidCheck(employee => employee.CompanyId)
            .HasNotEmptyGuidCheck(employee => employee.RoleId)
            .HasEnumerationCheck(employee => employee.Status);
    }
}
