namespace HealthyFac.Insights.Application.Chats.Tools;

/// <summary>
/// Reads the time zone of the company of the user, so the chat tool get_current_time can give the local time.
/// </summary>
public interface ICompanyTimeZoneReader
{
    /// <summary>
    /// Reads the time zone of the company of the current user.
    /// </summary>
    /// <param name="cancellationToken">The token that stops the call.</param>
    /// <returns>The IANA ID of the time zone.</returns>
    /// <exception cref="Ingest.Processing.Failures.ProviderUnavailableException">
    /// Thrown when Core does not answer.
    /// </exception>
    Task<string> ReadTimeZoneIdAsync(CancellationToken cancellationToken);
}
