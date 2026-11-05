using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.Machines.Persistence;

/// <summary>
/// Configures entity framework mapping for the machine state history entity. The unique index on the machine and
/// the start makes the latest row of a machine deterministic and serves the current state subquery.
/// </summary>
public class MachineStateHistoryConfiguration : IEntityTypeConfiguration<MachineStateHistory>
{
    public void Configure(EntityTypeBuilder<MachineStateHistory> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(row => row.State)
            .IsRequired();

        builder.HasIndex(row => new { row.MachineId, row.StartedAt })
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasOne<Machine>()
            .WithMany(machine => machine.StateHistory)
            .HasForeignKey(row => row.MachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasEnumerationCheck(row => row.State)
            .HasNotEmptyGuidCheck(row => row.MachineId);
    }
}
