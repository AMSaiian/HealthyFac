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
/// Configures entity framework mapping for the metric definition draft entity. The limits equal the limits of the
/// proposal message. A check constraint keeps exactly one target.
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

        builder.Property(draft => draft.QuantityKindName)
            .HasMaxLength(DraftMessageLimits.Metric.UnitNameMaxLength);

        builder.Property(draft => draft.UnitName)
            .HasMaxLength(DraftMessageLimits.Metric.UnitNameMaxLength);

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
            .HasDatabaseName(DraftIndexNames.MetricAttempt);

        builder.HasIndex(draft => new { draft.TargetMachineId, draft.State })
            .HasDatabaseName(DraftIndexNames.MetricTarget)
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasEnumerationCheck(draft => draft.State)
            .HasLengthCheck(draft => draft.SourceLocation, minLength: 0, DraftMessageLimits.SourceLocationMaxLength)
            .HasLengthCheck(draft => draft.Key, minLength: 1, DraftMessageLimits.Metric.KeyMaxLength)
            .HasLengthCheck(draft => draft.Name, minLength: 1, DraftMessageLimits.Metric.NameMaxLength)
            .HasLengthCheck(draft => draft.Description, minLength: 0, RichTextConstants.HtmlMaxLength)
            .HasNotEmptyGuidCheck(draft => draft.CompanyId);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(DraftCheckNames.MetricOneTarget, DraftCheckNames.OneTargetSql);
            table.HasCheckConstraint(DraftCheckNames.MetricRange, DraftCheckNames.RangeSql);
            table.HasCheckConstraint(
                DraftCheckNames.MetricValueKind,
                CheckConstraintSql.AllowedValues(column: "value_kind", DraftMessageLimits.ValueKinds.All));
        });
    }
}
