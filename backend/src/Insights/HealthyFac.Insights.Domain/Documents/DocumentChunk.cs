using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Documents;

/// <summary>
/// One chunk of an ingested document with its embedding. The embedding is a float array, so the Domain layer needs no
/// vector library. Infrastructure maps it to a <c>vector(1536)</c> column.
/// </summary>
public class DocumentChunk : IEntity
{
    /// <summary>
    /// Creates a chunk.
    /// </summary>
    /// <param name="documentId">The document of the chunk.</param>
    /// <param name="ordinal">The position of the chunk in the document, from 0.</param>
    /// <param name="location">The page, sheet, or slide and the heading path, or <see langword="null"/>.</param>
    /// <param name="text">The text of the chunk.</param>
    /// <param name="embedding">The unit length embedding of the text.</param>
    public DocumentChunk(Guid documentId, int ordinal, string? location, string text, float[] embedding)
    {
        DocumentId = documentId;
        Ordinal = ordinal;
        Location = location;
        Text = text;
        Embedding = embedding;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid DocumentId { get; private set; }
    public int Ordinal { get; private set; }
    public string? Location { get; private set; }
    public string Text { get; private set; }
    public float[] Embedding { get; private set; }
}
