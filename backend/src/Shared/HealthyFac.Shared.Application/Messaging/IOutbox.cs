namespace HealthyFac.Shared.Application.Messaging;

/// <summary>
/// Queues messages for other services inside the business transaction of a use case.
/// </summary>
public interface IOutbox
{
    /// <summary>
    /// Tracks an outbox row for <paramref name="message"/> on the scoped service context. The next
    /// <c>SaveChangesAsync</c> call on that context commits the row together with the business change, so the
    /// caller needs no explicit transaction.
    /// </summary>
    /// <typeparam name="TMessage">
    /// The message contract. Its <see cref="IIntegrationMessage.MessageType"/> becomes the stored type name.
    /// </typeparam>
    /// <param name="message">The message to send.</param>
    void Add<TMessage>(TMessage message) where TMessage : IIntegrationMessage;
}
