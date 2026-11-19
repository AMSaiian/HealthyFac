using System.Text.Json.Serialization;

namespace HealthyFac.Document.Infrastructure.Database.Seeding.DemoFiles;

/// <summary>One file ref of <c>document.json</c>. The keys are the database column names of the snapshot.</summary>
public sealed record DemoFileSeedRow
{
    [JsonPropertyName(name: "id")]
    public required Guid Id { get; init; }

    [JsonPropertyName(name: "machine_id")]
    public Guid? MachineId { get; init; }

    [JsonPropertyName(name: "purpose")]
    public required string Purpose { get; init; }

    [JsonPropertyName(name: "file_name")]
    public required string FileName { get; init; }

    [JsonPropertyName(name: "extension")]
    public required string Extension { get; init; }

    [JsonPropertyName(name: "content_type")]
    public required string ContentType { get; init; }

    [JsonPropertyName(name: "size_bytes")]
    public required long SizeBytes { get; init; }

    [JsonPropertyName(name: "sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName(name: "description")]
    public string? Description { get; init; }

    [JsonPropertyName(name: "ingest_failed_count")]
    public int IngestFailedCount { get; init; }

    [JsonPropertyName(name: "ingest_state")]
    public required string IngestState { get; init; }

    [JsonPropertyName(name: "ingest_state_changed_at")]
    public DateTime? IngestStateChangedAt { get; init; }

    [JsonPropertyName(name: "machine_changed_at")]
    public DateTime? MachineChangedAt { get; init; }
}
