using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

/// <summary>
/// Configures entity framework mapping for the metric definition draft entity. The limits equal the limits of the
/// proposal message. The draft has no foreign key to its proposal entry or to a machine registration, because either
/// row can arrive first.
/// </summary>
public class MetricDefinitionDraftConfiguration : IEntityTypeConfiguration<MetricDefinitionDraft>
{
    private const string StatesType = "text[]";

    public void Configure(EntityTypeBuilder<MetricDefinitionDraft> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(draft => draft.States)
            .HasColumnType(StatesType);

        builder.Property(draft => draft.State)
            .IsRequired();

        builder.Property(draft => draft.SourceLocation)
            .HasMaxLength(DraftMessageLimits.SourceLocationMaxLength);

        builder.Property(draft => draft.Key)
            .HasMaxLength(DraftMessageLimits.Metric.KeyMaxLength);

        builder.Property(draft => draft.Name)
            .HasMaxLength(DraftMessageLimits.Metric.NameMaxLength);

        builder.Property(draft => draft.ValueKind)
            .HasMaxLength(DraftMessageLimits.ValueKinds.All.Max(kind => kind.Length));

        builder.Property(draft => draft.Behavior)
            .HasMaxLength(DraftMessageLimits.Behaviors.All.Max(behavior => behavior.Length));

        builder.Property(draft => draft.QuantityKindName)
            .HasMaxLength(DraftMessageLimits.Metric.UnitNameMaxLength);

        builder.Property(draft => draft.UnitName)
            .HasMaxLength(DraftMessageLimits.Metric.UnitNameMaxLength);

        builder.ConfigureVersioned();

        builder.HasIndex(draft => new { draft.CompanyId, draft.AttemptId })
            .HasDatabaseName(DefinitionDraftIndexNames.MetricAttempt);

        builder.HasIndex(draft => new { draft.CompanyId, draft.TargetMachineId, draft.State })
            .HasDatabaseName(DefinitionDraftIndexNames.MetricTarget)
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasIndex(draft => draft.ProposalEntryId)
            .HasDatabaseName(DefinitionDraftIndexNames.MetricEntry);

        builder.HasEnumerationCheck(draft => draft.State)
            .HasLengthCheck(draft => draft.SourceLocation, minLength: 0, DraftMessageLimits.SourceLocationMaxLength)
            .HasLengthCheck(draft => draft.Key, minLength: 1, DraftMessageLimits.Metric.KeyMaxLength)
            .HasLengthCheck(draft => draft.Name, minLength: 1, DraftMessageLimits.Metric.NameMaxLength)
            .HasLengthCheck(draft => draft.Description, minLength: 0, RichTextConstants.HtmlMaxLength)
            .HasNotEmptyGuidCheck(draft => draft.CompanyId)
            .HasNotEmptyGuidCheck(draft => draft.ProposalEntryId);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(DefinitionDraftCheckNames.MetricTarget, DefinitionDraftCheckNames.TargetSql);
            table.HasCheckConstraint(DefinitionDraftCheckNames.MetricRange, DefinitionDraftCheckNames.RangeSql);
            table.HasCheckConstraint(
                DefinitionDraftCheckNames.MetricWarningLow, DefinitionDraftCheckNames.WarningLowSql);
            table.HasCheckConstraint(
                DefinitionDraftCheckNames.MetricWarningHigh,
                DefinitionDraftCheckNames.WarningHighSql);
            table.HasCheckConstraint(
                DefinitionDraftCheckNames.MetricBehavior,
                CheckConstraintSql.AllowedValues(column: "behavior", DraftMessageLimits.Behaviors.All));
            table.HasCheckConstraint(
                DefinitionDraftCheckNames.MetricValueKind,
                CheckConstraintSql.AllowedValues(column: "value_kind", DraftMessageLimits.ValueKinds.All));
        });
    }
}
