namespace HealthyFac.Shared.Application.UseCases;

/// <summary>
/// The base class of a use case without a result. It locks the step order: run the work, then return
/// <see cref="Unit"/>. The public <see cref="HandleAsync"/> is not virtual, so a derived class cannot skip the
/// locked steps. A derived class implements <see cref="HandleInternalAsync"/>.
/// </summary>
/// <remarks>
/// The class does not implement <see cref="IRequestHandler{TRequest}"/> on purpose. A raw handler under that
/// interface would skip the decorators. It implements <see cref="IRequestHandler{TRequest,TResponse}"/>, so the
/// scan registers it there and the decorators wrap it. Callers inject <see cref="IRequestHandler{TRequest}"/>, and
/// the shared adapter forwards the call to the decorated handler.
/// </remarks>
/// <typeparam name="TRequest">The type of the request that the handler accepts.</typeparam>
public abstract class RequestHandler<TRequest> : IRequestHandler<TRequest, Unit>
    where TRequest : IRequest
{
    /// <summary>
    /// Runs the work of the use case and returns <see cref="Unit"/>.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>The default <see cref="Unit"/>.</returns>
    public async Task<Unit> HandleAsync(TRequest request, CancellationToken ct = default)
    {
        await HandleInternalAsync(request, ct);

        return default;
    }

    /// <summary>
    /// Runs the work of the use case.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>A task that completes when the work ends.</returns>
    protected abstract Task HandleInternalAsync(TRequest request, CancellationToken ct);
}
