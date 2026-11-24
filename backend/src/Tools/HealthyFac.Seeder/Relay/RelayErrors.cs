namespace HealthyFac.Seeder.Relay;

/// <summary>
/// Defines the error messages of the message relay.
/// </summary>
public static class RelayErrors
{
    public static string MessageFailed(string type, string service, string eventItem, string error) =>
        $"The service {service} rejected the message {type} that the event {eventItem} caused: {error}";

    public static string NotProcessed(int attemptCount) =>
        $"The message is not processed after {attemptCount} attempts.";
}
