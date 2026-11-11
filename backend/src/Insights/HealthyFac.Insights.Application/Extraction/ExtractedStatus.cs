namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// One status, error, alarm, or warning code that the model found.
/// </summary>
public record ExtractedStatus
{
    public string? Code { get; init; }

    public ExtractedSeverity? Severity { get; init; }

    public string? Description { get; init; }

    public string? SourceLocation { get; init; }
}
