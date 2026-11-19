namespace HealthyFac.Insights.Application.Ingest.Processing.Failures;

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

    /// <summary>
    /// What the screening did before it failed, or <see langword="null"/> for another stage. The ingest handler stores
    /// it as a failed <c>screener</c> run.
    /// </summary>
    public Screening.ScreeningRunLog? ScreeningRun { get; set; }
}
