using HealthyFac.Core.Application.Roles;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.Roles.Persistence;

/// <summary>
/// Configures entity framework mapping for the role entity.
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureSoftDeletable();

        builder.Property(role => role.Code)
            .IsRequired();

        builder.Property(role => role.Name)
            .HasMaxLength(RoleValidationConstants.NameMaxLength)
            .IsRequired();

        builder.HasIndex(role => role.Code)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasEnumerationCheck(role => role.Code)
            .HasNotBlankCheck(role => role.Name);
    }
}
