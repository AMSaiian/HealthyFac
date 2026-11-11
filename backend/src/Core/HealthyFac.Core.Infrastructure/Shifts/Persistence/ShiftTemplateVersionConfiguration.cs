using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.Shifts.Persistence;

/// <summary>
/// Configures entity framework mapping for a version of the weekly shift template. An employee has one version for
/// each effective date.
/// </summary>
public class ShiftTemplateVersionConfiguration : IEntityTypeConfiguration<ShiftTemplateVersion>
{
    public void Configure(EntityTypeBuilder<ShiftTemplateVersion> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.ConfigureVersioned();

        builder.HasIndex(version => new { version.EmployeeId, version.EffectiveFrom })
            .HasDatabaseName(ShiftIndexNames.TemplateVersionEffectiveFrom)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(version => version.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasNotEmptyGuidCheck(version => version.EmployeeId);
    }
}
