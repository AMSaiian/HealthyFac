using HealthyFac.Shared.Infrastructure.Messaging.Transport;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Defines the exceptions that the outbox and the inbox throw.
/// </summary>
public static class MessagingErrors
{
    public static InvalidOperationException ActorMissing() =>
        new(
            "The current scope has no actor. A message carries the actor of the operation that sends it, "
            + "so set the actor before you add an outbox message."
        );

    public static InvalidIntegrationMessageException ActorHeaderMissing(string messageId) =>
        new($"Message '{messageId}' has no valid actor header, so it cannot run as any actor.");

    public static InvalidIntegrationMessageException MessageIdInvalid(string rawMessageId) =>
        new($"The message ID '{rawMessageId}' is not a GUID, so the inbox cannot use it as a key.");

    public static InvalidIntegrationMessageException CompanyHeaderInvalid(string messageId) =>
        new($"Message '{messageId}' has a company header that is not a GUID.");

    public static InvalidOperationException MessageTypeConflict(
        string messageType,
        Type registered,
        Type conflicting
    ) =>
        new(
            $"Message type '{messageType}' is already registered for '{registered.FullName}' and cannot also "
            + $"map to '{conflicting.FullName}'."
        );

    public static InvalidOperationException HandlerAlreadyRegistered(string messageType) =>
        new(
            $"Message type '{messageType}' already has a handler. Each message type has one handler per service. "
            + "When two steps must react to one message, one handler runs both, or it saves a new message "
            + "through the outbox."
        );

    public static InvalidOperationException MissingBrokerConnectionString() =>
        new($"The connection string '{MessagingConstants.RabbitMqConnectionStringName}' is missing or blank.");

    public static InvalidOperationException UnregisteredMessageType(string messageType) =>
        new(
            $"No handler is registered for message type '{messageType}'. "
            + "The row stays in the inbox and retries, so a later deployment can handle it."
        );

    public static InvalidIntegrationMessageException PayloadInvalid(string messageType, Exception? innerException)
    {
        var text = $"The payload of message type '{messageType}' is not valid JSON for that type.";

        return innerException is null
            ? new InvalidIntegrationMessageException(text)
            : new InvalidIntegrationMessageException(text, innerException);
    }

    /// <summary>
    /// Reports a payload that the <c>jsonb</c> column cannot store. The text never holds the payload, because it
    /// can hold personal data (e.g., an email), and the text goes to the <c>rbs2-error-details</c> header and to the
    /// logs.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="messageType">The stable contract name of the message.</param>
    /// <returns>The exception that moves the message to the error queue.</returns>
    public static InvalidIntegrationMessageException PayloadNotStorable(string messageId, string messageType) =>
        new(
            $"Message '{messageId}' of type '{messageType}' has a payload that the inbox cannot store: "
            + "it is not JSON text, or it holds a null character escape."
        );

    public static InvalidOperationException MessageHandlerMissing(string messageType) =>
        new(
            $"Message type '{messageType}' has no use case handler. "
            + "Register the use cases with AddUseCases before you call AddIntegrationMessage."
        );

    public static InvalidOperationException OutboxMessageAlreadyProcessed(Guid messageId) =>
        new($"Outbox message '{messageId}' is already published.");

    public static InvalidOperationException InboxMessageAlreadyProcessed(Guid messageId) =>
        new($"Inbox message '{messageId}' is already processed.");

    public static InvalidOperationException InboxMessageAlreadyFailed(Guid messageId) =>
        new($"Inbox message '{messageId}' is failed and is never handled.");

    public static InvalidOperationException InboxMessageNotTracked(Guid messageId) =>
        new(
            $"Inbox message '{messageId}' is not tracked by the scoped service context. "
            + "Load it through the inbox set of that context, so its processed state commits with the business change."
        );
}
