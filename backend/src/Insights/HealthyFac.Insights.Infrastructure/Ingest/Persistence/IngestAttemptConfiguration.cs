using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Insights.Infrastructure.Ingest.Persistence;

/// <summary>
/// Configures entity framework mapping for the ingest attempt entity. The row version (<c>xmin</c>) makes a cancel and
/// the worker conflict. Two unique indexes keep one active and one completed attempt for each file.
/// </summary>
public class IngestAttemptConfiguration : IEntityTypeConfiguration<IngestAttempt>
{
    private const int FileNameMaxLength = 255;
    private const int ExtensionMaxLength = 8;
    private const int TraceParentMaxLength = 55;
    private const int WarningMaxLength = 1000;

    private const string NotNegativeRule = "not_negative";
    private const string ErrorCodeRule = "only_when_failed";
    private const string InterruptReasonRule = "only_when_interrupted";
    private const string FinishedAtRule = "only_when_final";

    public void Configure(EntityTypeBuilder<IngestAttempt> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(attempt => attempt.FileName)
            .HasMaxLength(FileNameMaxLength)
            .IsRequired();

        builder.Property(attempt => attempt.Extension)
            .HasMaxLength(ExtensionMaxLength)
            .IsRequired();

        builder.Property(attempt => attempt.State)
            .IsRequired();

        builder.Property(attempt => attempt.TraceParent)
            .HasMaxLength(TraceParentMaxLength);

        builder.Property(attempt => attempt.Warning)
            .HasMaxLength(WarningMaxLength);

        builder.ConfigureVersioned();

        builder.HasIndex(attempt => attempt.FileId, IngestIndexNames.ActiveFile)
            .IsUnique()
            .HasFilter(
                $"state IN ('{IngestState.Queued.Value}', '{IngestState.InProcess.Value}') "
                + $"AND {SoftDeleteConstants.NotDeletedFilter}");

        builder.HasIndex(attempt => attempt.FileId, IngestIndexNames.CompletedFile)
            .IsUnique()
            .HasFilter($"state = '{IngestState.Completed.Value}' AND {SoftDeleteConstants.NotDeletedFilter}");

        builder.HasIndex(attempt => new { attempt.State, attempt.QueuedAt })
            .HasDatabaseName(IngestIndexNames.StateQueuedAt);

        builder.HasEnumerationCheck(attempt => attempt.State)
            .HasEnumerationCheck(attempt => attempt.ErrorCode)
            .HasEnumerationCheck(attempt => attempt.InterruptReason)
            .HasNotBlankCheck(attempt => attempt.FileName)
            .HasNotBlankCheck(attempt => attempt.Extension)
            .HasRangeCheck(attempt => attempt.SizeBytes, min: 0L, long.MaxValue)
            .HasNotEmptyGuidCheck(attempt => attempt.CompanyId)
            .HasNotEmptyGuidCheck(attempt => attempt.FileId);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                ConstraintName(nameof(IngestAttempt.PageCount), NotNegativeRule),
                sql: "page_count IS NULL OR page_count >= 0");
            table.HasCheckConstraint(
                ConstraintName(nameof(IngestAttempt.ChunkCount), NotNegativeRule),
                sql: "chunk_count IS NULL OR chunk_count >= 0");
            table.HasCheckConstraint(
                ConstraintName(nameof(IngestAttempt.ProposedDraftCount), NotNegativeRule),
                sql: "proposed_draft_count IS NULL OR proposed_draft_count >= 0");
            table.HasCheckConstraint(
                ConstraintName(nameof(IngestAttempt.ErrorCode), ErrorCodeRule),
                $"error_code IS NULL OR state = '{IngestState.Failed.Value}'");
            table.HasCheckConstraint(
                ConstraintName(nameof(IngestAttempt.InterruptReason), InterruptReasonRule),
                $"interrupt_reason IS NULL OR state = '{IngestState.Interrupted.Value}'");
            table.HasCheckConstraint(
                ConstraintName(nameof(IngestAttempt.FinishedAt), FinishedAtRule),
                $"finished_at IS NULL OR state NOT IN ('{IngestState.Queued.Value}', '{IngestState.InProcess.Value}')");
        });
    }

    private static string ConstraintName(string property, string rule) =>
        CheckConstraintSql.ConstraintName(nameof(IngestAttempt).ToSnakeCase(), property.ToSnakeCase(), rule);
}
