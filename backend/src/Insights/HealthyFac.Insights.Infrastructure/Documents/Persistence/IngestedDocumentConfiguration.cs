using HealthyFac.Insights.Domain.Documents;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Infrastructure.Ingest.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Insights.Infrastructure.Documents.Persistence;

/// <summary>
/// Configures entity framework mapping for the ingested document entity. The document has neither auditing nor soft
/// delete: a delete of the file removes the row, and the cascade removes the chunks.
/// </summary>
public class IngestedDocumentConfiguration : IEntityTypeConfiguration<IngestedDocument>
{
    private const int ExtensionMaxLength = 8;

    public void Configure(EntityTypeBuilder<IngestedDocument> builder)
    {
        builder.ConfigureEntity();

        builder.Property(document => document.Extension)
            .HasMaxLength(ExtensionMaxLength)
            .IsRequired();

        builder.HasOne<IngestAttempt>()
            .WithMany()
            .HasForeignKey(document => document.AttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(document => document.Chunks)
            .WithOne()
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(document => new { document.CompanyId, document.MachineId })
            .HasDatabaseName(IngestIndexNames.DocumentScope);

        builder.HasNotBlankCheck(document => document.Extension)
            .HasRangeCheck(document => document.PageCount, min: 0, int.MaxValue)
            .HasRangeCheck(document => document.ChunkCount, min: 0, int.MaxValue)
            .HasNotEmptyGuidCheck(document => document.CompanyId);
    }
}
