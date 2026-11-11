namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// Reports that the model answer does not match the response schema, also after the repair turn.
/// </summary>
public sealed class InvalidExtractionException : Exception
{
    public InvalidExtractionException()
        : base()
    {
    }

    public InvalidExtractionException(string message)
        : base(message)
    {
    }

    public InvalidExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
