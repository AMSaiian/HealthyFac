using System.Text.Json;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Infrastructure.Messaging.Processing;

/// <summary>
/// Turns a stored inbox row into a call of its typed use case. The dispatcher is scoped, so the use case it
/// resolves shares the scope, the service context, and the transaction of the processor.
/// </summary>
/// <param name="registry">The message types that the service handles.</param>
/// <param name="services">The service provider of the current scope.</param>
internal sealed class IntegrationMessageDispatcher(IntegrationMessageRegistry registry, IServiceProvider services)
{
    /// <summary>
    /// Calls the typed dispatch that is registered for the message type.
    /// </summary>
    /// <param name="type">The stable contract name of the message.</param>
    /// <param name="payload">The message serialized as JSON.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the use case finishes.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no use case is registered for <paramref name="type"/>. The processor then records a failure,
    /// so a deployment that adds the handler before the attempt limit still handles the message.
    /// </exception>
    public Task DispatchAsync(string type, string payload, CancellationToken cancellationToken)
    {
        if (!registry.TryGetDispatch(type, out var dispatch))
        {
            throw MessagingErrors.UnregisteredMessageType(type);
        }

        return dispatch(services, payload, cancellationToken);
    }

    /// <summary>
    /// Deserializes the payload and calls the use case of <typeparamref name="TMessage"/>. The use case runs through
    /// the validation and logging decorators. The registry stores this method as the dispatch delegate of the type,
    /// so no reflection runs for each message.
    /// </summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="services">The service provider of the scope that runs the message.</param>
    /// <param name="payload">The message serialized as JSON.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the use case finishes.</returns>
    /// <exception cref="InvalidIntegrationMessageException">
    /// Thrown when the payload is not a valid JSON document of <typeparamref name="TMessage"/>. No retry can fix
    /// it, so the processor marks the row failed at the first attempt.
    /// </exception>
    internal static async Task DispatchTypedAsync<TMessage>(
        IServiceProvider services,
        string payload,
        CancellationToken cancellationToken
    )
        where TMessage : IIntegrationMessage, IRequest
    {
        var message = Deserialize<TMessage>(payload);

        await services.GetRequiredService<IRequestHandler<TMessage>>().HandleAsync(message, cancellationToken);
    }

    private static TMessage Deserialize<TMessage>(string payload)
        where TMessage : IIntegrationMessage
    {
        try
        {
            return JsonSerializer.Deserialize<TMessage>(payload, JsonSerializerOptions.Web)
                ?? throw MessagingErrors.PayloadInvalid(TMessage.MessageType, null);
        }
        catch (JsonException exception)
        {
            throw MessagingErrors.PayloadInvalid(TMessage.MessageType, exception);
        }
    }
}
