using HealthyFac.Telemetry.Domain.DefinitionDrafts;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

/// <summary>
/// A metric definition draft with all its values.
/// </summary>
public record MetricDraftDetail
{
    public required Guid Id { get; init; }

    public required DefinitionDraftState State { get; init; }

    public required Guid ProposalEntryId { get; init; }

    public Guid? TargetMachineId { get; init; }

    public Guid? TargetMachineDraftId { get; init; }

    public string? SourceLocation { get; init; }

    public string? Key { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? ValueKind { get; init; }

    public string? QuantityKindName { get; init; }

    public string? UnitName { get; init; }

    public double? Min { get; init; }

    public double? Max { get; init; }

    public IReadOnlyList<string>? States { get; init; }

    public static MetricDraftDetail From(MetricDefinitionDraft draft) =>
        new()
        {
            Id = draft.Id,
            State = draft.State,
            ProposalEntryId = draft.ProposalEntryId,
            TargetMachineId = draft.TargetMachineId,
            TargetMachineDraftId = draft.TargetMachineDraftId,
            SourceLocation = draft.SourceLocation,
            Key = draft.Key,
            Name = draft.Name,
            Description = draft.Description,
            ValueKind = draft.ValueKind,
            QuantityKindName = draft.QuantityKindName,
            UnitName = draft.UnitName,
            Min = draft.Min,
            Max = draft.Max,
            States = draft.States,
        };
}

/// <summary>
/// A status definition draft with all its values.
/// </summary>
public record StatusDraftDetail
{
    public required Guid Id { get; init; }

    public required DefinitionDraftState State { get; init; }

    public required Guid ProposalEntryId { get; init; }

    public Guid? TargetMachineId { get; init; }

    public Guid? TargetMachineDraftId { get; init; }

    public string? SourceLocation { get; init; }

    public string? Code { get; init; }

    public string? Severity { get; init; }

    public string? Description { get; init; }

    public static StatusDraftDetail From(StatusDefinitionDraft draft) =>
        new()
        {
            Id = draft.Id,
            State = draft.State,
            ProposalEntryId = draft.ProposalEntryId,
            TargetMachineId = draft.TargetMachineId,
            TargetMachineDraftId = draft.TargetMachineDraftId,
            SourceLocation = draft.SourceLocation,
            Code = draft.Code,
            Severity = draft.Severity,
            Description = draft.Description,
        };
}
