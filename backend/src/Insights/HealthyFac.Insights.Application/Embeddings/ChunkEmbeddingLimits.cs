namespace HealthyFac.Insights.Application.Embeddings;

/// <summary>
/// The size of an embedding and the number of parallel embedding calls. Infrastructure fills them from its options,
/// so the Application layer needs no reference to the options class.
/// </summary>
public record ChunkEmbeddingLimits
{
    public required int Dimensions { get; init; }

    public required int Parallelism { get; init; }
}
