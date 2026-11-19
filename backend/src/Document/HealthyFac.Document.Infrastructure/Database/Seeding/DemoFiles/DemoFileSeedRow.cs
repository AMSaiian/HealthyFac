using System.Text.Json.Serialization;

namespace HealthyFac.Document.Infrastructure.Database.Seeding;

/// <summary>One file ref of <c>document.json</c>. The keys are the database column names of the snapshot.</summary>
public sealed record DemoFileSeedRow
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("machine_id")]
    public Guid? MachineId { get; init; }

    [JsonPropertyName("purpose")]
    public required string Purpose { get; init; }

    [JsonPropertyName("file_name")]
    public required string FileName { get; init; }

    [JsonPropertyName("extension")]
    public required string Extension { get; init; }

    [JsonPropertyName("content_type")]
    public required string ContentType { get; init; }

    [JsonPropertyName("size_bytes")]
    public required long SizeBytes { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("ingest_failed_count")]
    public int IngestFailedCount { get; init; }

    [JsonPropertyName("ingest_state")]
    public required string IngestState { get; init; }

    [JsonPropertyName("ingest_state_changed_at")]
    public DateTime? IngestStateChangedAt { get; init; }

    [JsonPropertyName("machine_changed_at")]
    public DateTime? MachineChangedAt { get; init; }
}
