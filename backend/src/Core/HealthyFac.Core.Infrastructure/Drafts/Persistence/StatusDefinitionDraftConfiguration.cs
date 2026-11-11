using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.Drafts.Persistence;

/// <summary>
/// Configures entity framework mapping for the status definition draft entity. The limits equal the limits of the
/// proposal message. A check constraint keeps exactly one target.
/// </summary>
public class StatusDefinitionDraftConfiguration : IEntityTypeConfiguration<StatusDefinitionDraft>
{
    public void Configure(EntityTypeBuilder<StatusDefinitionDraft> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(draft => draft.State)
            .IsRequired();

        builder.Property(draft => draft.SourceLocation)
            .HasMaxLength(DraftMessageLimits.SourceLocationMaxLength);

        builder.Property(draft => draft.Code)
            .HasMaxLength(DraftMessageLimits.Status.CodeMaxLength);

        builder.Property(draft => draft.Severity)
            .HasMaxLength(DraftMessageLimits.Severities.All.Max(severity => severity.Length));

        builder.ConfigureVersioned();

        builder.HasOne<MachineDraft>()
            .WithMany()
            .HasForeignKey(draft => draft.TargetMachineDraftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Machine>()
            .WithMany()
            .HasForeignKey(draft => draft.TargetMachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(draft => new { draft.CompanyId, draft.AttemptId })
            .HasDatabaseName(DraftIndexNames.StatusAttempt);

        builder.HasIndex(draft => new { draft.TargetMachineId, draft.State })
            .HasDatabaseName(DraftIndexNames.StatusTarget)
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasEnumerationCheck(draft => draft.State)
            .HasLengthCheck(draft => draft.SourceLocation, minLength: 0, DraftMessageLimits.SourceLocationMaxLength)
            .HasLengthCheck(draft => draft.Code, minLength: 1, DraftMessageLimits.Status.CodeMaxLength)
            .HasLengthCheck(draft => draft.Description, minLength: 0, RichTextConstants.HtmlMaxLength)
            .HasNotEmptyGuidCheck(draft => draft.CompanyId);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(DraftCheckNames.StatusOneTarget, DraftCheckNames.OneTargetSql);
            table.HasCheckConstraint(
                DraftCheckNames.StatusSeverity,
                CheckConstraintSql.AllowedValues(column: "severity", DraftMessageLimits.Severities.All));
        });
    }
}
