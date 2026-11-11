using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.Shifts.Persistence;

/// <summary>
/// Configures entity framework mapping for the shift override of one employee on one date.
/// </summary>
public class ShiftOverrideConfiguration : IEntityTypeConfiguration<ShiftOverride>
{
    public void Configure(EntityTypeBuilder<ShiftOverride> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.ConfigureVersioned();

        builder.HasIndex(shiftOverride => new { shiftOverride.EmployeeId, shiftOverride.Date })
            .HasDatabaseName(ShiftIndexNames.OverrideDate)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(shiftOverride => shiftOverride.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasNotEmptyGuidCheck(shiftOverride => shiftOverride.EmployeeId);
    }
}
