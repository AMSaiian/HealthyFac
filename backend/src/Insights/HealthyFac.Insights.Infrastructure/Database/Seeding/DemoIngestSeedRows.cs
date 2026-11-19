using System.Text.Json;
using System.Text.Json.Serialization;

namespace HealthyFac.Insights.Infrastructure.Database.Seeding;

/// <summary>The root of <c>insights.json</c>. The keys are the database column names of the snapshot.</summary>
public sealed record DemoIngestSeedFile
{
    [JsonPropertyName(name: "referenceTime")]
    public required DateTimeOffset ReferenceTime { get; init; }

    [JsonPropertyName(name: "ingestAttempts")]
    public required IReadOnlyList<DemoAttemptRow> IngestAttempts { get; init; }

    [JsonPropertyName(name: "ingestedDocuments")]
    public required IReadOnlyList<DemoDocumentRow> IngestedDocuments { get; init; }

    [JsonPropertyName(name: "documentChunks")]
    public required IReadOnlyList<DemoChunkRow> DocumentChunks { get; init; }

    [JsonPropertyName(name: "agentRuns")]
    public required IReadOnlyList<DemoRunRow> AgentRuns { get; init; }

    [JsonPropertyName(name: "agentToolCalls")]
    public required IReadOnlyList<DemoToolCallRow> AgentToolCalls { get; init; }
}

public sealed record DemoAttemptRow
{
    [JsonPropertyName(name: "id")]
    public required Guid Id { get; init; }

    [JsonPropertyName(name: "file_id")]
    public required Guid FileId { get; init; }

    [JsonPropertyName(name: "machine_id")]
    public Guid? MachineId { get; init; }

    [JsonPropertyName(name: "machine_changed_at")]
    public required DateTime MachineChangedAt { get; init; }

    [JsonPropertyName(name: "file_name")]
    public required string FileName { get; init; }

    [JsonPropertyName(name: "extension")]
    public required string Extension { get; init; }

    [JsonPropertyName(name: "size_bytes")]
    public required long SizeBytes { get; init; }

    [JsonPropertyName(name: "state")]
    public required string State { get; init; }

    [JsonPropertyName(name: "queued_at")]
    public required DateTime QueuedAt { get; init; }

    [JsonPropertyName(name: "started_at")]
    public required DateTime StartedAt { get; init; }

    [JsonPropertyName(name: "finished_at")]
    public required DateTime FinishedAt { get; init; }

    [JsonPropertyName(name: "page_count")]
    public required int PageCount { get; init; }

    [JsonPropertyName(name: "chunk_count")]
    public required int ChunkCount { get; init; }

    [JsonPropertyName(name: "proposed_draft_count")]
    public required int ProposedDraftCount { get; init; }
}

public sealed record DemoDocumentRow
{
    [JsonPropertyName(name: "id")]
    public required Guid Id { get; init; }

    [JsonPropertyName(name: "machine_id")]
    public Guid? MachineId { get; init; }

    [JsonPropertyName(name: "machine_changed_at")]
    public required DateTime MachineChangedAt { get; init; }

    [JsonPropertyName(name: "attempt_id")]
    public required Guid AttemptId { get; init; }

    [JsonPropertyName(name: "extension")]
    public required string Extension { get; init; }

    [JsonPropertyName(name: "page_count")]
    public required int PageCount { get; init; }
}

public sealed record DemoChunkRow
{
    [JsonPropertyName(name: "id")]
    public required Guid Id { get; init; }

    [JsonPropertyName(name: "text")]
    public required string Text { get; init; }

    [JsonPropertyName(name: "ordinal")]
    public required int Ordinal { get; init; }

    [JsonPropertyName(name: "location")]
    public string? Location { get; init; }

    [JsonPropertyName(name: "document_id")]
    public required Guid DocumentId { get; init; }
}

public sealed record DemoRunRow
{
    [JsonPropertyName(name: "id")]
    public required Guid Id { get; init; }

    [JsonPropertyName(name: "role")]
    public required string Role { get; init; }

    [JsonPropertyName(name: "ingest_attempt_id")]
    public required Guid IngestAttemptId { get; init; }

    [JsonPropertyName(name: "model")]
    public required string Model { get; init; }

    [JsonPropertyName(name: "prompt_version")]
    public required string PromptVersion { get; init; }

    [JsonPropertyName(name: "prompt_hash")]
    public required string PromptHash { get; init; }

    [JsonPropertyName(name: "state")]
    public required string State { get; init; }

    [JsonPropertyName(name: "due_at")]
    public required DateTime DueAt { get; init; }

    [JsonPropertyName(name: "started_at")]
    public required DateTime StartedAt { get; init; }

    [JsonPropertyName(name: "finished_at")]
    public required DateTime FinishedAt { get; init; }

    [JsonPropertyName(name: "input_tokens")]
    public required int InputTokens { get; init; }

    [JsonPropertyName(name: "output_tokens")]
    public required int OutputTokens { get; init; }
}

public sealed record DemoToolCallRow
{
    [JsonPropertyName(name: "id")]
    public required Guid Id { get; init; }

    [JsonPropertyName(name: "run_id")]
    public required Guid RunId { get; init; }

    [JsonPropertyName(name: "sequence")]
    public required int Sequence { get; init; }

    [JsonPropertyName(name: "call_id")]
    public required string CallId { get; init; }

    [JsonPropertyName(name: "tool_name")]
    public required string ToolName { get; init; }

    [JsonPropertyName(name: "arguments")]
    public required JsonElement Arguments { get; init; }

    [JsonPropertyName(name: "outcome")]
    public required string Outcome { get; init; }

    [JsonPropertyName(name: "result")]
    public required JsonElement Result { get; init; }

    [JsonPropertyName(name: "duration_milliseconds")]
    public required int DurationMilliseconds { get; init; }
}

/// <summary>The root of <c>insights-embeddings.json</c>.</summary>
public sealed record DemoEmbeddingsFile
{
    [JsonPropertyName(name: "dimensions")]
    public required int Dimensions { get; init; }

    [JsonPropertyName(name: "embeddings")]
    public required IReadOnlyDictionary<Guid, string> Embeddings { get; init; }
}
