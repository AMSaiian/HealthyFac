using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HealthyFac.Core.Application.Roles;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;

namespace HealthyFac.Core.Infrastructure.Roles.Persistence;

/// <summary>
/// Configures entity framework mapping for the role entity.
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ConfigureEntity();

        builder.Property(role => role.Code)
            .HasMaxLength(RoleValidationConstants.CodeMaxLength)
            .IsRequired();

        builder.Property(role => role.Name)
            .HasMaxLength(RoleValidationConstants.NameMaxLength)
            .IsRequired();

        builder.HasIndex(role => role.Code)
            .IsUnique();

        builder.HasNotBlankCheck(role => role.Code)
            .HasNotBlankCheck(role => role.Name);
    }
}
