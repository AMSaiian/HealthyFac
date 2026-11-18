using System.Text.Json.Serialization;

namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>The root of <c>telemetry.json</c>. The keys are the database column names of the snapshot.</summary>
public sealed record DemoDefinitionDraftSeedFile
{
    [JsonPropertyName(name: "referenceTime")]
    public required DateTimeOffset ReferenceTime { get; init; }

    [JsonPropertyName(name: "metricDefinitionDrafts")]
    public required IReadOnlyList<DemoMetricDraftRow> MetricDefinitionDrafts { get; init; }

    [JsonPropertyName(name: "statusDefinitionDrafts")]
    public required IReadOnlyList<DemoStatusDraftRow> StatusDefinitionDrafts { get; init; }
}

public sealed record DemoMetricDraftRow
{
    [JsonPropertyName(name: "id")]
    public required Guid Id { get; init; }

    [JsonPropertyName(name: "attempt_id")]
    public required Guid AttemptId { get; init; }

    [JsonPropertyName(name: "file_id")]
    public required Guid FileId { get; init; }

    [JsonPropertyName(name: "proposal_entry_id")]
    public required Guid ProposalEntryId { get; init; }

    [JsonPropertyName(name: "target_machine_draft_id")]
    public Guid? TargetMachineDraftId { get; init; }

    [JsonPropertyName(name: "target_machine_id")]
    public Guid? TargetMachineId { get; init; }

    [JsonPropertyName(name: "source_location")]
    public string? SourceLocation { get; init; }

    [JsonPropertyName(name: "key")]
    public string? Key { get; init; }

    [JsonPropertyName(name: "name")]
    public string? Name { get; init; }

    [JsonPropertyName(name: "description")]
    public string? Description { get; init; }

    [JsonPropertyName(name: "value_kind")]
    public string? ValueKind { get; init; }

    [JsonPropertyName(name: "quantity_kind_name")]
    public string? QuantityKindName { get; init; }

    [JsonPropertyName(name: "unit_name")]
    public string? UnitName { get; init; }

    [JsonPropertyName(name: "min")]
    public double? Min { get; init; }

    [JsonPropertyName(name: "max")]
    public double? Max { get; init; }

    [JsonPropertyName(name: "states")]
    public IReadOnlyList<string>? States { get; init; }
}

public sealed record DemoStatusDraftRow
{
    [JsonPropertyName(name: "id")]
    public required Guid Id { get; init; }

    [JsonPropertyName(name: "attempt_id")]
    public required Guid AttemptId { get; init; }

    [JsonPropertyName(name: "file_id")]
    public required Guid FileId { get; init; }

    [JsonPropertyName(name: "proposal_entry_id")]
    public required Guid ProposalEntryId { get; init; }

    [JsonPropertyName(name: "target_machine_draft_id")]
    public Guid? TargetMachineDraftId { get; init; }

    [JsonPropertyName(name: "target_machine_id")]
    public Guid? TargetMachineId { get; init; }

    [JsonPropertyName(name: "source_location")]
    public string? SourceLocation { get; init; }

    [JsonPropertyName(name: "code")]
    public string? Code { get; init; }

    [JsonPropertyName(name: "severity")]
    public string? Severity { get; init; }

    [JsonPropertyName(name: "description")]
    public string? Description { get; init; }
}
