using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// Tells a failure that a repeat can fix from one that it cannot.
/// </summary>
internal static class IntakeFailures
{
    /// <summary>
    /// Tells whether a later delivery of the same message can succeed: the database is not reachable, a command timed
    /// out, or the retry strategy gave up. Any other exception is the same on every delivery.
    /// </summary>
    /// <param name="exception">The failure of the intake use case.</param>
    /// <returns><see langword="true"/> when the failure is transient.</returns>
    public static bool IsTransient(Exception exception) =>
        exception switch
        {
            NpgsqlException { IsTransient: true } => true,
            TimeoutException => true,
            RetryLimitExceededException => true,
            DbUpdateException { InnerException: { } inner } => IsTransient(inner),
            _ => false,
        };
}
