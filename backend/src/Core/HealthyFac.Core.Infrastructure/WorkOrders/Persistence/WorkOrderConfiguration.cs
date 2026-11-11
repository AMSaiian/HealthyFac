using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.WorkOrders.Persistence;

/// <summary>
/// Configures entity framework mapping for the work order entity. The model declares no relationship from the current
/// revision to the history: a deferrable foreign key in the migration checks it at commit.
/// </summary>
public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    private const string EstimateCheckName = "ck_work_orders_estimate_range";

    private static readonly string EstimateCheck =
        $"estimate >= interval '{WorkOrderValidationConstants.EstimateMinMinutes} minutes' "
        + $"AND estimate <= interval '{WorkOrderValidationConstants.EstimateMaxMinutes} minutes'";

    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(workOrder => workOrder.Title)
            .HasMaxLength(WorkOrderValidationConstants.TitleMaxLength)
            .IsRequired();

        builder.Property(workOrder => workOrder.Priority)
            .IsRequired();

        builder.Property(workOrder => workOrder.Category)
            .IsRequired();

        builder.Property(workOrder => workOrder.Estimate)
            .IsRequired();

        builder.Property(workOrder => workOrder.CurrentRevision)
            .IsRequired();

        builder.ConfigureVersioned();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(workOrder => workOrder.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Machine>()
            .WithMany()
            .HasForeignKey(workOrder => workOrder.MachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasNotBlankCheck(workOrder => workOrder.Title)
            .HasLengthCheck(workOrder => workOrder.Title, minLength: 1, WorkOrderValidationConstants.TitleMaxLength)
            .HasLengthCheck(workOrder => workOrder.Description, minLength: 0, RichTextConstants.HtmlMaxLength)
            .HasEnumerationCheck(workOrder => workOrder.Priority)
            .HasEnumerationCheck(workOrder => workOrder.Category)
            .HasNotEmptyGuidCheck(workOrder => workOrder.CompanyId)
            .HasRangeCheck(workOrder => workOrder.CurrentRevision, min: 1, int.MaxValue);

        builder.ToTable(table => table.HasCheckConstraint(EstimateCheckName, EstimateCheck));
    }
}
