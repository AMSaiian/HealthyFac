using HealthyFac.Shared.Infrastructure.Messaging.Persistence;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Defines how a failed outbox or inbox message waits before its next attempt. The outbox row and the inbox row
/// call it, so both retry the same way.
/// </summary>
internal static class MessageRetryPolicy
{
    /// <summary>
    /// Calculates the earliest time of the next attempt. The delay doubles with each attempt, starting at two
    /// seconds, and never exceeds <see cref="MessagingConstants.MaxRetryDelaySeconds"/>.
    /// </summary>
    /// <param name="now">The UTC time of the failure.</param>
    /// <param name="attemptCount">The number of attempts, including the one that failed.</param>
    /// <returns>The UTC time after which the processor may try again.</returns>
    public static DateTime NextAttemptOn(DateTime now, int attemptCount) =>
        now.AddSeconds(Math.Min(Math.Pow(2, attemptCount), MessagingConstants.MaxRetryDelaySeconds));

    /// <summary>
    /// Cuts an error text to the size of the error column.
    /// </summary>
    /// <param name="error">The text of the failure.</param>
    /// <returns>The text, or its first <see cref="MessagingPersistenceConstants.ErrorMaxLength"/> characters.</returns>
    public static string TruncateError(string error) =>
        error.Length <= MessagingPersistenceConstants.ErrorMaxLength
            ? error
            : error[..MessagingPersistenceConstants.ErrorMaxLength];
}
