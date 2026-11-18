using System.Text.Json.Serialization;

namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>The root of <c>core.json</c>. The keys are the database column names of the snapshot.</summary>
public sealed record DemoDraftSeedFile
{
    [JsonPropertyName(name: "referenceTime")]
    public required DateTimeOffset ReferenceTime { get; init; }

    [JsonPropertyName(name: "machineDrafts")]
    public required IReadOnlyList<DemoMachineDraftRow> MachineDrafts { get; init; }
}

public sealed record DemoMachineDraftRow
{
    [JsonPropertyName(name: "id")]
    public required Guid Id { get; init; }

    [JsonPropertyName(name: "attempt_id")]
    public required Guid AttemptId { get; init; }

    [JsonPropertyName(name: "file_id")]
    public required Guid FileId { get; init; }

    [JsonPropertyName(name: "queued_at")]
    public required DateTime QueuedAt { get; init; }

    [JsonPropertyName(name: "target_machine_id")]
    public Guid? TargetMachineId { get; init; }

    [JsonPropertyName(name: "state")]
    public required string State { get; init; }

    [JsonPropertyName(name: "name")]
    public string? Name { get; init; }

    [JsonPropertyName(name: "serial_number")]
    public string? SerialNumber { get; init; }

    [JsonPropertyName(name: "description")]
    public string? Description { get; init; }
}
