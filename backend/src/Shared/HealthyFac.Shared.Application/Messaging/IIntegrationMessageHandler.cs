namespace HealthyFac.Shared.Application.Messaging;

/// <summary>
/// Handles one integration message that another service (or the same service) sent through the broker.
/// The inbox processor calls it once per message, inside one database transaction. The handler runs as the
/// original actor of the message, never checks permissions again, and must keep a payload company equal to
/// the actor company unless the actor is the system actor.
/// </summary>
/// <typeparam name="TMessage">The message contract that the handler accepts.</typeparam>
public interface IIntegrationMessageHandler<in TMessage>
    where TMessage : IIntegrationMessage
{
    /// <summary>
    /// Handles the message. Changes saved by the handler commit together with the processed state of the inbox
    /// row. An exception rolls them back and schedules a retry.
    /// </summary>
    /// <param name="message">The deserialized message.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the handler finishes.</returns>
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}
