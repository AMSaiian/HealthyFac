namespace HealthyFac.Seeder.Ingest;

/// <summary>
/// The content of <c>ingest/embeddings.json</c>: the vector of each chunk as base64 of float32 values.
/// </summary>
public sealed class IngestEmbeddingsFile
{
    public required IReadOnlyDictionary<Guid, string> Vectors { get; init; }
}
