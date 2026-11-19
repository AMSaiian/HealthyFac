namespace HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;

/// <summary>
/// The proposed values of a metric definition draft. Every value can be <see langword="null"/>.
/// </summary>
public sealed record MetricDraftValues
{
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

    /// <summary>One of <c>level</c>, <c>counter</c>, or <see langword="null"/>.</summary>
    public string? Behavior { get; init; }
}
