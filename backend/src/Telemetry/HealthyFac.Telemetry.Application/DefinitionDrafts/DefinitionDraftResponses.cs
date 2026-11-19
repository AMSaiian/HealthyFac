using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.StatusDrafts;

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

    public double? WarningLow { get; init; }

    public double? WarningHigh { get; init; }

    public double? AlarmLow { get; init; }

    public double? AlarmHigh { get; init; }

    public string? Behavior { get; init; }

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
            WarningLow = draft.WarningLow,
            WarningHigh = draft.WarningHigh,
            AlarmLow = draft.AlarmLow,
            AlarmHigh = draft.AlarmHigh,
            Behavior = draft.Behavior,
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

/// <summary>
/// An operating phase draft with all its values, conditions, and limit rows. A source is a definition or a draft of a
/// definition: the draft ID is set while the user has not applied that draft.
/// </summary>
public record PhaseDraftDetail
{
    public required Guid Id { get; init; }

    public required DefinitionDraftState State { get; init; }

    public required Guid ProposalEntryId { get; init; }

    public Guid? TargetMachineId { get; init; }

    public Guid? TargetMachineDraftId { get; init; }

    public string? SourceLocation { get; init; }

    public string? Code { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public int? Priority { get; init; }

    public bool? IsTracked { get; init; }

    public required IReadOnlyList<PhaseDraftConditionDetail> Conditions { get; init; }

    public required IReadOnlyList<PhaseDraftLimitDetail> Limits { get; init; }

    public static PhaseDraftDetail From(OperatingPhaseDraft draft) =>
        new()
        {
            Id = draft.Id,
            State = draft.State,
            ProposalEntryId = draft.ProposalEntryId,
            TargetMachineId = draft.TargetMachineId,
            TargetMachineDraftId = draft.TargetMachineDraftId,
            SourceLocation = draft.SourceLocation,
            Code = draft.Code,
            Name = draft.Name,
            Description = draft.Description,
            Priority = draft.Priority,
            IsTracked = draft.IsTracked,
            Conditions = [.. draft.Conditions.Select(PhaseDraftConditionDetail.From)],
            Limits = [.. draft.Limits.Select(PhaseDraftLimitDetail.From)],
        };
}

public record PhaseDraftConditionDetail
{
    public Guid? MetricDefinitionId { get; init; }

    public Guid? MetricDraftId { get; init; }

    public Guid? StatusDefinitionId { get; init; }

    public Guid? StatusDraftId { get; init; }

    public string? Operator { get; init; }

    public double? Value { get; init; }

    public double? UpperValue { get; init; }

    public IReadOnlyList<string>? States { get; init; }

    public static PhaseDraftConditionDetail From(OperatingPhaseDraftCondition condition) =>
        new()
        {
            MetricDefinitionId = condition.MetricDefinitionId,
            MetricDraftId = condition.MetricDraftId,
            StatusDefinitionId = condition.StatusDefinitionId,
            StatusDraftId = condition.StatusDraftId,
            Operator = condition.Operator,
            Value = condition.Value,
            UpperValue = condition.UpperValue,
            States = condition.States,
        };
}

public record PhaseDraftLimitDetail
{
    public Guid? MetricDefinitionId { get; init; }

    public Guid? MetricDraftId { get; init; }

    public double? WarningLow { get; init; }

    public double? WarningHigh { get; init; }

    public double? AlarmLow { get; init; }

    public double? AlarmHigh { get; init; }

    public static PhaseDraftLimitDetail From(OperatingPhaseDraftLimit limit) =>
        new()
        {
            MetricDefinitionId = limit.MetricDefinitionId,
            MetricDraftId = limit.MetricDraftId,
            WarningLow = limit.WarningLow,
            WarningHigh = limit.WarningHigh,
            AlarmLow = limit.AlarmLow,
            AlarmHigh = limit.AlarmHigh,
        };
}
