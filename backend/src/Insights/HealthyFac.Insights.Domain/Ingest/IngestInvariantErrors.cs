namespace HealthyFac.Insights.Domain.Ingest;

/// <summary>
/// Defines the exceptions that an attempt throws when a caller breaks the lifecycle. The Application layer checks the
/// state first, so these exceptions mark a programming error.
/// </summary>
public static class IngestInvariantErrors
{
    public static InvalidOperationException IllegalTransition(Guid attemptId, IngestState from, string action) =>
        new($"Ingest attempt '{attemptId}' is {from.Value}, so it cannot {action}.");
}
