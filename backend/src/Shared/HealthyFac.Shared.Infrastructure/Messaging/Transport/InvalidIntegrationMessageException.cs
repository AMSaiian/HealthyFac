namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// A message that no retry can fix. At receive, for example a message without a valid actor header, Rebus moves
/// it to the <c>error</c> queue at the first attempt. In the inbox, for example a payload that does not
/// deserialize, the processor marks the row failed at the first attempt.
/// </summary>
public sealed class InvalidIntegrationMessageException : Exception
{
    public InvalidIntegrationMessageException(string message)
        : base(message)
    {
    }

    public InvalidIntegrationMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
