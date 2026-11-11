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
/// Configures entity framework mapping for the employee invite entity. A company holds at most one open invite
/// for an email, so a resend renews the existing invite instead of adding a second one.
/// </summary>
public class EmployeeInviteConfiguration : IEntityTypeConfiguration<EmployeeInvite>
{
    public void Configure(EntityTypeBuilder<EmployeeInvite> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(invite => invite.Email)
            .HasMaxLength(EmployeeValidationConstants.EmailMaxLength)
            .IsRequired();

        builder.Property(invite => invite.FirstName)
            .HasMaxLength(EmployeeValidationConstants.NameMaxLength);

        builder.Property(invite => invite.LastName)
            .HasMaxLength(EmployeeValidationConstants.NameMaxLength);

        builder.Property(invite => invite.TokenSalt)
            .IsRequired();

        builder.ConfigureVersioned();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(invite => invite.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(invite => invite.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(invite => new { invite.CompanyId, invite.Email })
            .IsUnique()
            .HasFilter($"accepted_at IS NULL AND {SoftDeleteConstants.NotDeletedFilter}");

        builder.HasNotBlankCheck(invite => invite.Email)
            .HasNotBlankCheck(invite => invite.FirstName)
            .HasNotBlankCheck(invite => invite.LastName)
            .HasNotEmptyGuidCheck(invite => invite.CompanyId)
            .HasNotEmptyGuidCheck(invite => invite.RoleId);
    }
}
