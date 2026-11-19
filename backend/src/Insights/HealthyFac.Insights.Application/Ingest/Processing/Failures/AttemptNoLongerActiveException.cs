namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// Reports that the attempt left the state <c>inProcess</c> while the pipeline ran (a user cancel, a delete of the
/// file, or a sweep). The run stops without a save.
/// </summary>
public sealed class AttemptNoLongerActiveException : Exception
{
    public AttemptNoLongerActiveException()
        : base()
    {
    }

    public AttemptNoLongerActiveException(string message)
        : base(message)
    {
    }

    public AttemptNoLongerActiveException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
