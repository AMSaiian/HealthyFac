using System.Text.Json.Serialization;

namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>The root of <c>core.json</c>. The keys are the database column names of the snapshot.</summary>
public sealed record DemoDraftSeedFile
{
    [JsonPropertyName("referenceTime")]
    public required DateTimeOffset ReferenceTime { get; init; }

    [JsonPropertyName("machineDrafts")]
    public required IReadOnlyList<DemoMachineDraftRow> MachineDrafts { get; init; }

    [JsonPropertyName("metricDefinitionDrafts")]
    public required IReadOnlyList<DemoMetricDraftRow> MetricDefinitionDrafts { get; init; }

    [JsonPropertyName("statusDefinitionDrafts")]
    public required IReadOnlyList<DemoStatusDraftRow> StatusDefinitionDrafts { get; init; }
}

public sealed record DemoMachineDraftRow
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("attempt_id")]
    public required Guid AttemptId { get; init; }

    [JsonPropertyName("file_id")]
    public required Guid FileId { get; init; }

    [JsonPropertyName("queued_at")]
    public required DateTime QueuedAt { get; init; }

    [JsonPropertyName("target_machine_id")]
    public Guid? TargetMachineId { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("serial_number")]
    public string? SerialNumber { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

public sealed record DemoMetricDraftRow
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("attempt_id")]
    public required Guid AttemptId { get; init; }

    [JsonPropertyName("file_id")]
    public required Guid FileId { get; init; }

    [JsonPropertyName("target_machine_draft_id")]
    public Guid? TargetMachineDraftId { get; init; }

    [JsonPropertyName("target_machine_id")]
    public Guid? TargetMachineId { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("source_location")]
    public string? SourceLocation { get; init; }

    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("value_kind")]
    public string? ValueKind { get; init; }

    [JsonPropertyName("quantity_kind_name")]
    public string? QuantityKindName { get; init; }

    [JsonPropertyName("unit_name")]
    public string? UnitName { get; init; }

    [JsonPropertyName("min")]
    public double? Min { get; init; }

    [JsonPropertyName("max")]
    public double? Max { get; init; }

    [JsonPropertyName("states")]
    public IReadOnlyList<string>? States { get; init; }
}

public sealed record DemoStatusDraftRow
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("attempt_id")]
    public required Guid AttemptId { get; init; }

    [JsonPropertyName("file_id")]
    public required Guid FileId { get; init; }

    [JsonPropertyName("target_machine_draft_id")]
    public Guid? TargetMachineDraftId { get; init; }

    [JsonPropertyName("target_machine_id")]
    public Guid? TargetMachineId { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("source_location")]
    public string? SourceLocation { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("severity")]
    public string? Severity { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
