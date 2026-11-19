namespace HealthyFac.Telemetry.Domain.DefinitionDrafts;

/// <summary>
/// The proposed values of an operating phase draft. Every value can be <see langword="null"/>.
/// </summary>
public sealed record PhaseDraftValues
{
    public string? SourceLocation { get; init; }
    public string? Code { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Priority { get; init; }
    public bool? IsTracked { get; init; }
}

/// <summary>
/// The proposed condition of a phase draft. A source is a definition or a draft of a definition: exactly one of the
/// four IDs is set.
/// </summary>
public sealed record PhaseDraftConditionValues
{
    public Guid? MetricDefinitionId { get; init; }
    public Guid? MetricDraftId { get; init; }
    public Guid? StatusDefinitionId { get; init; }
    public Guid? StatusDraftId { get; init; }
    public string? Operator { get; init; }
    public double? Value { get; init; }
    public double? UpperValue { get; init; }
    public IReadOnlyList<string>? States { get; init; }
}

/// <summary>
/// The proposed limit row of a phase draft. The metric is a definition or a draft of a definition: exactly one of
/// the two IDs is set.
/// </summary>
public sealed record PhaseDraftLimitValues
{
    public Guid? MetricDefinitionId { get; init; }
    public Guid? MetricDraftId { get; init; }
    public double? WarningLow { get; init; }
    public double? WarningHigh { get; init; }
    public double? AlarmLow { get; init; }
    public double? AlarmHigh { get; init; }
}
