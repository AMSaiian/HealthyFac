using System.Text.Json.Serialization;

namespace HealthyFac.Insights.Infrastructure.Database.Seeding;

/// <summary>The root of <c>insights.json</c>. The keys are the database column names of the snapshot.</summary>
public sealed record DemoIngestSeedFile
{
    [JsonPropertyName("referenceTime")]
    public required DateTimeOffset ReferenceTime { get; init; }

    [JsonPropertyName("ingestAttempts")]
    public required IReadOnlyList<DemoAttemptRow> IngestAttempts { get; init; }

    [JsonPropertyName("ingestedDocuments")]
    public required IReadOnlyList<DemoDocumentRow> IngestedDocuments { get; init; }

    [JsonPropertyName("documentChunks")]
    public required IReadOnlyList<DemoChunkRow> DocumentChunks { get; init; }
}

public sealed record DemoAttemptRow
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("file_id")]
    public required Guid FileId { get; init; }

    [JsonPropertyName("machine_id")]
    public Guid? MachineId { get; init; }

    [JsonPropertyName("machine_changed_at")]
    public required DateTime MachineChangedAt { get; init; }

    [JsonPropertyName("file_name")]
    public required string FileName { get; init; }

    [JsonPropertyName("extension")]
    public required string Extension { get; init; }

    [JsonPropertyName("size_bytes")]
    public required long SizeBytes { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("queued_at")]
    public required DateTime QueuedAt { get; init; }

    [JsonPropertyName("started_at")]
    public required DateTime StartedAt { get; init; }

    [JsonPropertyName("finished_at")]
    public required DateTime FinishedAt { get; init; }

    [JsonPropertyName("page_count")]
    public required int PageCount { get; init; }

    [JsonPropertyName("chunk_count")]
    public required int ChunkCount { get; init; }

    [JsonPropertyName("proposed_draft_count")]
    public required int ProposedDraftCount { get; init; }
}

public sealed record DemoDocumentRow
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("machine_id")]
    public Guid? MachineId { get; init; }

    [JsonPropertyName("machine_changed_at")]
    public required DateTime MachineChangedAt { get; init; }

    [JsonPropertyName("attempt_id")]
    public required Guid AttemptId { get; init; }

    [JsonPropertyName("extension")]
    public required string Extension { get; init; }

    [JsonPropertyName("page_count")]
    public required int PageCount { get; init; }
}

public sealed record DemoChunkRow
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("ordinal")]
    public required int Ordinal { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("document_id")]
    public required Guid DocumentId { get; init; }
}

/// <summary>The root of <c>insights-embeddings.json</c>.</summary>
public sealed record DemoEmbeddingsFile
{
    [JsonPropertyName("dimensions")]
    public required int Dimensions { get; init; }

    [JsonPropertyName("embeddings")]
    public required IReadOnlyDictionary<Guid, string> Embeddings { get; init; }
}
