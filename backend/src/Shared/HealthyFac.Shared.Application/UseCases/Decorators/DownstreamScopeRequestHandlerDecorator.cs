using System.Reflection;
using HealthyFac.Shared.Application.DownstreamScopes;

namespace HealthyFac.Shared.Application.UseCases.Decorators;

/// <summary>
/// Decorates a request handler to enter the downstream scopes that the request declares with
/// <see cref="DownstreamScopesAttribute"/> around the handler. The downstream scopes end after the handler, also after
/// an exception. A request without the attribute runs with no downstream scopes, also inside a flow that has
/// downstream scopes, so a nested use case never inherits the downstream scopes of its parent.
/// </summary>
internal sealed class DownstreamScopeRequestHandlerDecorator<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> innerHandler,
    IDownstreamScopeHolder holder
) : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IRequestHandler<TRequest, TResponse> _innerHandler = innerHandler;
    private readonly IDownstreamScopeHolder _holder = holder;

    private static readonly string[]? Declared = typeof(TRequest)
        .GetCustomAttribute<DownstreamScopesAttribute>()
        ?.Scopes
        .ToArray();

    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken ct = default(CancellationToken))
    {
        using var downstreamScope = _holder.Enter(Declared ?? []);

        return await _innerHandler.HandleAsync(request, ct);
    }
}
