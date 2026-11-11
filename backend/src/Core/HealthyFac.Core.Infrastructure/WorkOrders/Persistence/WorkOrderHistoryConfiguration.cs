using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.WorkOrders.Persistence;

/// <summary>
/// Configures entity framework mapping for the work order history. A revision is never reused, so the key
/// <c>(work_order_id, revision)</c> keeps deleted rows. The deferrable foreign key of the current revision in the
/// migration points at it.
/// </summary>
public class WorkOrderHistoryConfiguration : IEntityTypeConfiguration<WorkOrderHistory>
{
    private const string LocalColumnType = "timestamp without time zone";
    private const string UtcColumnType = "timestamp with time zone";

    public void Configure(EntityTypeBuilder<WorkOrderHistory> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(row => row.Status)
            .IsRequired();

        builder.Property(row => row.PlannedStartLocal)
            .HasColumnType(LocalColumnType);

        builder.Property(row => row.PlannedStartUtc)
            .HasColumnType(UtcColumnType);

        builder.HasAlternateKey(row => new { row.WorkOrderId, row.Revision })
            .HasName(WorkOrderIndexNames.HistoryRevision);

        builder.HasIndex(row => new { row.AssigneeId, row.PlannedStartUtc })
            .HasDatabaseName(WorkOrderIndexNames.HistoryAssigneePlannedStart);

        builder.HasOne<WorkOrder>()
            .WithMany(workOrder => workOrder.History)
            .HasForeignKey(row => row.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(row => row.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasEnumerationCheck(row => row.Status)
            .HasNotEmptyGuidCheck(row => row.WorkOrderId)
            .HasRangeCheck(row => row.Revision, min: 1, int.MaxValue);
    }
}
