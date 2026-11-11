namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// One machine that the model found with its metrics and statuses.
/// </summary>
public record ExtractedMachine
{
    public string? Name { get; init; }

    public string? SerialNumber { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<ExtractedMetric> Metrics { get; init; } = [];

    public IReadOnlyList<ExtractedStatus> Statuses { get; init; } = [];
}
