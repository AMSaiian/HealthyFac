using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.Messaging.Processing;

namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// Deserializes a stored payload and calls the typed use case of one message type.
/// </summary>
/// <param name="services">The service provider of the scope that runs the message.</param>
/// <param name="payload">The message serialized as JSON.</param>
/// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
/// <returns>A task that completes when the use case finishes.</returns>
public delegate Task IntegrationMessageDispatch(
    IServiceProvider services,
    string payload,
    CancellationToken cancellationToken
);

/// <summary>
/// Maps the contract name of each message that the service handles to its typed dispatch. The transport
/// subscribes the input queue to every registered name, and the dispatcher calls the delegate of the name.
/// Registration runs while the host builds its services, and the processors only read the registry afterwards.
/// </summary>
public sealed class IntegrationMessageRegistry
{
    private readonly Dictionary<string, (Type MessageClrType, IntegrationMessageDispatch Dispatch)> _registrations =
        new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> MessageTypes => _registrations.Keys;

    /// <summary>
    /// Adds a message contract and its typed dispatch. A service has exactly one use case for each message type:
    /// when two steps must react to one message, one use case runs both, or it saves a new message through the
    /// outbox.
    /// </summary>
    /// <typeparam name="TMessage">The message contract. Its <see cref="IIntegrationMessage.MessageType"/> is the
    /// registered name.</typeparam>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the name of <typeparamref name="TMessage"/> is already registered. The same CLR type means a
    /// second registration, which changes nothing. Another CLR type means that a payload would deserialize to the
    /// wrong contract.
    /// </exception>
    public void Register<TMessage>()
        where TMessage : IIntegrationMessage, IRequest
    {
        var messageType = TMessage.MessageType;
        if (_registrations.TryGetValue(messageType, out var registration))
        {
            throw registration.MessageClrType == typeof(TMessage)
                ? MessagingErrors.HandlerAlreadyRegistered(messageType)
                : MessagingErrors.MessageTypeConflict(messageType, registration.MessageClrType, typeof(TMessage));
        }

        _registrations[messageType] = (typeof(TMessage), IntegrationMessageDispatcher.DispatchTypedAsync<TMessage>);
    }

    public bool TryGetDispatch(string messageType, out IntegrationMessageDispatch dispatch)
    {
        var isRegistered = _registrations.TryGetValue(messageType, out var registration);
        dispatch = registration.Dispatch;

        return isRegistered;
    }
}
