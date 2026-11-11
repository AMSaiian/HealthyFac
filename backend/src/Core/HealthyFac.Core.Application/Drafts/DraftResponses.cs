using HealthyFac.Core.Domain.Drafts;

namespace HealthyFac.Core.Application.Drafts;

/// <summary>
/// A machine draft with all its values.
/// </summary>
public record MachineDraftDetail
{
    public required Guid Id { get; init; }

    public required string State { get; init; }

    public required bool IsCreate { get; init; }

    public required Guid AttemptId { get; init; }

    public required Guid FileId { get; init; }

    public string? Name { get; init; }

    public string? SerialNumber { get; init; }

    public string? Description { get; init; }

    /// <summary>The row version of the draft.</summary>
    public required uint RowVersion { get; init; }

    public static MachineDraftDetail From(MachineDraft draft) =>
        new()
        {
            Id = draft.Id,
            State = draft.State.Value,
            IsCreate = draft.IsCreate,
            AttemptId = draft.AttemptId,
            FileId = draft.FileId,
            Name = draft.Name,
            SerialNumber = draft.SerialNumber,
            Description = draft.Description,
            RowVersion = draft.RowVersion,
        };
}

/// <summary>
/// A metric definition draft with all its values.
/// </summary>
public record MetricDraftDetail
{
    public required Guid Id { get; init; }

    public required string State { get; init; }

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
            State = draft.State.Value,
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

    public required string State { get; init; }

    public string? SourceLocation { get; init; }

    public string? Code { get; init; }

    public string? Severity { get; init; }

    public string? Description { get; init; }

    public static StatusDraftDetail From(StatusDefinitionDraft draft) =>
        new()
        {
            Id = draft.Id,
            State = draft.State.Value,
            SourceLocation = draft.SourceLocation,
            Code = draft.Code,
            Severity = draft.Severity,
            Description = draft.Description,
        };
}
