using HealthyFac.Insights.Domain.Documents;
using HealthyFac.Insights.Infrastructure.Ingest.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.Collations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace HealthyFac.Insights.Infrastructure.Documents.Persistence;

/// <summary>
/// Configures entity framework mapping for the document chunk entity. The embedding column is a
/// <c>vector(1536)</c> with an HNSW index for the cosine distance. The text compares case-sensitively, because a
/// chunk is source text and not a name.
/// </summary>
public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public const int EmbeddingDimensions = 1536;

    private const string EmbeddingColumnType = "vector(1536)";
    private const string HnswMethod = "hnsw";
    private const string CosineOperators = "vector_cosine_ops";
    private const string MaxConnectionsParameter = "m";
    private const string ConstructionListParameter = "ef_construction";
    private const int MaxConnections = 16;
    private const int ConstructionList = 64;

    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ConfigureEntity();

        builder.Property(chunk => chunk.Text)
            .IsRequired()
            .IsCaseSensitive();

        builder.Property(chunk => chunk.Embedding)
            .HasColumnType(EmbeddingColumnType)
            .HasConversion(
                embedding => new Vector(embedding),
                vector => vector.ToArray());

        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.Ordinal })
            .HasDatabaseName(IngestIndexNames.ChunkOrdinal)
            .IsUnique();

        builder.HasIndex(chunk => chunk.Embedding)
            .HasDatabaseName(IngestIndexNames.ChunkEmbedding)
            .HasMethod(HnswMethod)
            .HasOperators(CosineOperators)
            .HasStorageParameter(MaxConnectionsParameter, MaxConnections)
            .HasStorageParameter(ConstructionListParameter, ConstructionList);
    }
}
