using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

/// <summary>
/// Configures entity framework mapping for the operating phase draft entity. The limits equal the limits of the
/// proposal message. The draft has no foreign key to its proposal entry or to a machine registration, because either
/// row can arrive first. The conditions and the limit rows are child entities with a cascade: a loaded child
/// is soft deleted with its draft, and a hard delete removes them.
/// </summary>
public class OperatingPhaseDraftConfiguration : IEntityTypeConfiguration<OperatingPhaseDraft>
{
    public const string DraftIdName = "DraftId";

    public void Configure(EntityTypeBuilder<OperatingPhaseDraft> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.ConfigureVersioned();

        builder.Property(draft => draft.State)
            .IsRequired();

        builder.Property(draft => draft.SourceLocation)
            .HasMaxLength(DraftMessageLimits.SourceLocationMaxLength);

        builder.Property(draft => draft.Code)
            .HasMaxLength(DraftMessageLimits.Phase.CodeMaxLength);

        builder.Property(draft => draft.Name)
            .HasMaxLength(DraftMessageLimits.Phase.NameMaxLength);

        builder.HasMany(draft => draft.Conditions)
            .WithOne()
            .HasForeignKey(DraftIdName)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(draft => draft.Limits)
            .WithOne()
            .HasForeignKey(DraftIdName)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(draft => draft.Proposal)
            .WithMany()
            .HasForeignKey(draft => draft.ProposalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(draft => new { draft.CompanyId, draft.ProposalId })
            .HasDatabaseName(DefinitionDraftIndexNames.PhaseProposal);

        builder.HasIndex(draft => new { draft.CompanyId, draft.TargetMachineId, draft.State })
            .HasDatabaseName(DefinitionDraftIndexNames.PhaseTarget)
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasIndex(draft => draft.ProposalEntryId)
            .HasDatabaseName(DefinitionDraftIndexNames.PhaseEntry);

        builder.HasEnumerationCheck(draft => draft.State)
            .HasLengthCheck(draft => draft.SourceLocation, minLength: 0, DraftMessageLimits.SourceLocationMaxLength)
            .HasLengthCheck(draft => draft.Code, minLength: 1, DraftMessageLimits.Phase.CodeMaxLength)
            .HasLengthCheck(draft => draft.Name, minLength: 1, DraftMessageLimits.Phase.NameMaxLength)
            .HasLengthCheck(draft => draft.Description, minLength: 0, RichTextConstants.HtmlMaxLength)
            .HasNotEmptyGuidCheck(draft => draft.CompanyId)
            .HasNotEmptyGuidCheck(draft => draft.ProposalId)
            .HasNotEmptyGuidCheck(draft => draft.ProposalEntryId);

        builder.ToTable(table =>
            table.HasCheckConstraint(DefinitionDraftCheckNames.PhaseTarget, DefinitionDraftCheckNames.TargetSql));
    }
}
