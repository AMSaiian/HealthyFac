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
/// Configures entity framework mapping for the employee invite entity. A company holds at most one open invite
/// for an email, so a resend renews the existing invite instead of adding a second one. The department links
/// carry the company and reference both sides together with it, so the database rejects a link to a
/// department of another company.
/// </summary>
public class EmployeeInviteConfiguration : IEntityTypeConfiguration<EmployeeInvite>
{
    public void Configure(EntityTypeBuilder<EmployeeInvite> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();

        builder.Property(invite => invite.Email)
            .HasMaxLength(EmployeeValidationConstants.EmailMaxLength)
            .IsRequired();

        builder.Property(invite => invite.FirstName)
            .HasMaxLength(EmployeeValidationConstants.NameMaxLength);

        builder.Property(invite => invite.LastName)
            .HasMaxLength(EmployeeValidationConstants.NameMaxLength);

        builder.Property(invite => invite.TokenSalt)
            .IsRequired();

        builder.Property<uint>(PersistenceConstants.ConcurrencyToken)
            .IsRowVersion();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(invite => invite.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(invite => invite.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(invite => new { invite.Id, invite.CompanyId });

        builder.HasMany(invite => invite.Departments)
            .WithMany()
            .UsingEntity(
                "employee_invite_departments",
                department => department.HasOne(typeof(Department))
                    .WithMany()
                    .HasForeignKey(nameof(Department) + nameof(Department.Id), nameof(Department.CompanyId))
                    .HasPrincipalKey(nameof(Department.Id), nameof(Department.CompanyId))
                    .OnDelete(DeleteBehavior.Restrict),
                invite => invite.HasOne(typeof(EmployeeInvite))
                    .WithMany()
                    .HasForeignKey(nameof(EmployeeInvite) + nameof(EmployeeInvite.Id), nameof(EmployeeInvite.CompanyId))
                    .HasPrincipalKey(nameof(EmployeeInvite.Id), nameof(EmployeeInvite.CompanyId))
                    .OnDelete(DeleteBehavior.Cascade),
                join => join.HasKey(
                    nameof(EmployeeInvite) + nameof(EmployeeInvite.Id),
                    nameof(Department) + nameof(Department.Id)));

        builder.HasIndex(invite => new { invite.CompanyId, invite.Email })
            .IsUnique()
            .HasFilter("accepted_at IS NULL");

        builder.HasNotBlankCheck(invite => invite.Email)
            .HasNotBlankCheck(invite => invite.FirstName)
            .HasNotBlankCheck(invite => invite.LastName)
            .HasNotEmptyGuidCheck(invite => invite.CompanyId)
            .HasNotEmptyGuidCheck(invite => invite.RoleId);
    }
}
