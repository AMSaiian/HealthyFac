namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// The kind of value of a metric that the model found. The JSON values are the camelCase names of the members.
/// </summary>
public enum ExtractedValueKind
{
    Numeric,
    Boolean,
    StateSet,
}
