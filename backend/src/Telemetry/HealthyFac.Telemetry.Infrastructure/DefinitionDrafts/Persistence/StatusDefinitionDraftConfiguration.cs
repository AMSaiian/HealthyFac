using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

/// <summary>
/// Configures entity framework mapping for the status definition draft entity. The limits equal the limits of the
/// proposal message. The draft has no foreign key to its proposal entry or to a machine registration, because either
/// row can arrive first.
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

        builder.HasIndex(draft => new { draft.CompanyId, draft.AttemptId })
            .HasDatabaseName(DefinitionDraftIndexNames.StatusAttempt);

        builder.HasIndex(draft => new { draft.CompanyId, draft.TargetMachineId, draft.State })
            .HasDatabaseName(DefinitionDraftIndexNames.StatusTarget)
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasIndex(draft => draft.ProposalEntryId)
            .HasDatabaseName(DefinitionDraftIndexNames.StatusEntry);

        builder.HasEnumerationCheck(draft => draft.State)
            .HasLengthCheck(draft => draft.SourceLocation, minLength: 0, DraftMessageLimits.SourceLocationMaxLength)
            .HasLengthCheck(draft => draft.Code, minLength: 1, DraftMessageLimits.Status.CodeMaxLength)
            .HasLengthCheck(draft => draft.Description, minLength: 0, RichTextConstants.HtmlMaxLength)
            .HasNotEmptyGuidCheck(draft => draft.CompanyId)
            .HasNotEmptyGuidCheck(draft => draft.ProposalEntryId);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(DefinitionDraftCheckNames.StatusTarget, DefinitionDraftCheckNames.TargetSql);
            table.HasCheckConstraint(
                DefinitionDraftCheckNames.StatusSeverity,
                CheckConstraintSql.AllowedValues(column: "severity", DraftMessageLimits.Severities.All));
        });
    }
}
