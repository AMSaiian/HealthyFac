using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.WorkOrders.Persistence;

/// <summary>
/// Configures entity framework mapping for the link between a work order and a file. The file lives in the Document
/// service, so the file ID has no foreign key.
/// </summary>
public class WorkOrderFileConfiguration : IEntityTypeConfiguration<WorkOrderFile>
{
    public void Configure(EntityTypeBuilder<WorkOrderFile> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.HasIndex(link => new { link.WorkOrderId, link.FileId })
            .HasDatabaseName(WorkOrderIndexNames.FileLink)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasOne<WorkOrder>()
            .WithMany(workOrder => workOrder.Files)
            .HasForeignKey(link => link.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasNotEmptyGuidCheck(link => link.WorkOrderId)
            .HasNotEmptyGuidCheck(link => link.FileId);
    }
}
