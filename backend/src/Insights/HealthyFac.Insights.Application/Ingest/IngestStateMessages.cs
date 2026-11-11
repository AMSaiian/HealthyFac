using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Messaging.Ingest;

namespace HealthyFac.Insights.Application.Ingest;

/// <summary>
/// Builds the <see cref="IngestStateChanged"/> message of an attempt, so every use case that moves an attempt tells
/// Document the same way.
/// </summary>
public static class IngestStateMessages
{
    /// <summary>
    /// Builds the message for the current state of an attempt.
    /// </summary>
    /// <param name="attempt">The attempt after the change.</param>
    /// <param name="failedCount">The number of attempts of the file that ended as failed, including this one.</param>
    /// <param name="changedAt">The time of the change (UTC).</param>
    /// <returns>The message for the outbox.</returns>
    public static IngestStateChanged Create(IngestAttempt attempt, int failedCount, DateTime changedAt) =>
        new()
        {
            FileId = attempt.FileId,
            CompanyId = attempt.CompanyId,
            State = attempt.State.Value,
            FailedCount = failedCount,
            ChangedAt = changedAt,
        };
}
