namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// One metric that the model found. Every value can be null, because the document may not state it. The record holds no
/// ID, no target, no company, and no file: the mapper adds them.
/// </summary>
public record ExtractedMetric
{
    public string? Key { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public ExtractedValueKind? ValueKind { get; init; }

    public string? QuantityKind { get; init; }

    public string? Unit { get; init; }

    public double? Min { get; init; }

    public double? Max { get; init; }

    public IReadOnlyList<string>? States { get; init; }

    public string? SourceLocation { get; init; }
}
