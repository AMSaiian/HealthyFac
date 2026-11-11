namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// Reports that an external service stayed unavailable through all retries.
/// </summary>
public sealed class ProviderUnavailableException : Exception
{
    public ProviderUnavailableException()
        : base()
    {
    }

    public ProviderUnavailableException(string message)
        : base(message)
    {
    }

    public ProviderUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
