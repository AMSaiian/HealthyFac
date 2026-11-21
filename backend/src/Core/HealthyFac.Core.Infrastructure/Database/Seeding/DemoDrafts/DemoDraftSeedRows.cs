using System.Text.Json.Serialization;

namespace HealthyFac.Core.Infrastructure.Database.Seeding.DemoDrafts;

/// <summary>The root of <c>core.json</c>. The keys are the database column names of the snapshot.</summary>
public sealed record DemoDraftSeedFile
{
    [JsonPropertyName(name: "referenceTime")]
    public required DateTimeOffset ReferenceTime { get; init; }

    [JsonPropertyName(name: "machineDrafts")]
    public required IReadOnlyList<DemoMachineDraftRow> MachineDrafts { get; init; }

    [JsonPropertyName(name: "workOrderDrafts")]
    public required IReadOnlyList<DemoWorkOrderDraftRow> WorkOrderDrafts { get; init; }
}

public sealed record DemoWorkOrderDraftRow
{
    [JsonPropertyName(name: "id")]
    public required Guid Id { get; init; }

    [JsonPropertyName(name: "machine_id")]
    public required Guid MachineId { get; init; }

    [JsonPropertyName(name: "case_id")]
    public required Guid CaseId { get; init; }

    [JsonPropertyName(name: "run_id")]
    public required Guid RunId { get; init; }

    [JsonPropertyName(name: "title")]
    public required string Title { get; init; }

    [JsonPropertyName(name: "description")]
    public string? Description { get; init; }

    [JsonPropertyName(name: "priority")]
    public required string Priority { get; init; }

    [JsonPropertyName(name: "category")]
    public required string Category { get; init; }

    [JsonPropertyName(name: "estimate_minutes")]
    public required int EstimateMinutes { get; init; }
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
