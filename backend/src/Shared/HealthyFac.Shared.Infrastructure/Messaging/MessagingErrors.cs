namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Defines the exceptions that the outbox and the inbox throw.
/// </summary>
public static class MessagingErrors
{
    public static InvalidOperationException InboxMessageAlreadyProcessed(Guid messageId) =>
        new($"Inbox message '{messageId}' is already processed.");

    public static InvalidOperationException InboxMessageNotTracked(Guid messageId) =>
        new(
            $"Inbox message '{messageId}' is not tracked by the scoped service context. "
            + "Load it through the inbox set of that context, so its processed state commits with the business change."
        );
}
