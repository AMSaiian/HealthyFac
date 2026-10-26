namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// A received message that no retry can fix, for example a message without a valid actor header. Rebus moves it
/// to the <c>error</c> queue at the first attempt.
/// </summary>
public sealed class InvalidIntegrationMessageException : Exception
{
    public InvalidIntegrationMessageException(string message)
        : base(message)
    {
    }
}
