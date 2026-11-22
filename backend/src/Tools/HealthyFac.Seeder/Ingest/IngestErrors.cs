namespace HealthyFac.Seeder.Ingest;

/// <summary>Defines the error messages of the ingest part.</summary>
public static class IngestErrors
{
    public static string ContentTypeUnknown(string item, string extension) =>
        $"The event {item} uploads a file with the extension {extension}, which has no content type.";

    public static string VectorMissing(Guid chunkId) =>
        $"The file ingest/embeddings.json holds no vector for the chunk {chunkId}.";
}
