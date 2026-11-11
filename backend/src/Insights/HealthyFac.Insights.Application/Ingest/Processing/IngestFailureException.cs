using HealthyFac.Insights.Domain.Ingest;

namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// Reports that a stage of the pipeline failed with a known error code. The use case turns it into a failed attempt.
/// </summary>
public sealed class IngestFailureException : Exception
{
    public IngestFailureException(IngestErrorCode code)
        : base(code.Value)
    {
        Code = code;
    }

    public IngestFailureException(IngestErrorCode code, Exception innerException)
        : base(code.Value, innerException)
    {
        Code = code;
    }

    public IngestErrorCode Code { get; }
}
