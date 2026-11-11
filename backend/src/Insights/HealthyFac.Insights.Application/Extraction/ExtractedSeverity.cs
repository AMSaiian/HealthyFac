namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// The severity of a status that the model found. The JSON values are the camelCase names of the members.
/// </summary>
public enum ExtractedSeverity
{
    Info,
    Warning,
    Error,
}
