using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Shared.UnitTests.Messaging.Data;

/// <summary>
/// Stands in for the use case of a registry test message. <c>AddIntegrationMessage</c> only checks that a handler
/// is registered, so the handler never runs.
/// </summary>
/// <typeparam name="TMessage">The test message.</typeparam>
public sealed class RegistryMessageHandler<TMessage> : IRequestHandler<TMessage, Unit>
    where TMessage : IRequest
{
    public Task<Unit> HandleAsync(TMessage request, CancellationToken cancellationToken = default(CancellationToken))
        => Task.FromResult(default(Unit));
}
