using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.WorkOrders.Drafts;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.WorkOrders.Drafts.Persistence;

/// <summary>
/// Configures the work order draft table. The text is the plain text that the planner proposed, so its length check
/// uses the work order description limit.
/// </summary>
public class WorkOrderDraftConfiguration : IEntityTypeConfiguration<WorkOrderDraft>
{
    public const string MachineStateIndex = "ix_work_order_drafts_company_id_machine_id_state";

    public void Configure(EntityTypeBuilder<WorkOrderDraft> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.ConfigureVersioned();

        builder.Property(draft => draft.Title)
            .HasMaxLength(WorkOrderValidationConstants.TitleMaxLength)
            .IsRequired();

        builder.Property(draft => draft.State)
            .IsRequired();

        builder.HasOne<Machine>()
            .WithMany()
            .HasForeignKey(draft => draft.MachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(draft => new { draft.CompanyId, draft.MachineId, draft.State })
            .HasDatabaseName(MachineStateIndex)
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasEnumerationCheck(draft => draft.State)
            .HasEnumerationCheck(draft => draft.Priority)
            .HasEnumerationCheck(draft => draft.Category)
            .HasNotBlankCheck(draft => draft.Title)
            .HasLengthCheck(draft => draft.Title, minLength: 1, WorkOrderValidationConstants.TitleMaxLength)
            .HasLengthCheck(
                draft => draft.Description, minLength: 0, WorkOrderValidationConstants.DescriptionMaxLength)
            .HasRangeCheck(
                draft => draft.EstimateMinutes,
                WorkOrderValidationConstants.EstimateMinMinutes,
                WorkOrderValidationConstants.EstimateMaxMinutes)
            .HasNotEmptyGuidCheck(draft => draft.CompanyId);
    }
}
