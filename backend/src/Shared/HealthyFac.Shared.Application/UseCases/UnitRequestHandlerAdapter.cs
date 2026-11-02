namespace HealthyFac.Shared.Application.UseCases;

/// <summary>
/// Gives a caller the <see cref="IRequestHandler{TRequest}"/> of a use case without a result. The adapter calls
/// the decorated pipeline, so the validation and logging decorators run for every call.
/// </summary>
/// <typeparam name="TRequest">The type of the request that the handler accepts.</typeparam>
/// <param name="pipeline">The decorated handler of the request.</param>
internal sealed class UnitRequestHandlerAdapter<TRequest>(IRequestHandler<TRequest, Unit> pipeline)
    : IRequestHandler<TRequest>
    where TRequest : IRequest
{
    private readonly IRequestHandler<TRequest, Unit> _pipeline = pipeline;

    public Task HandleAsync(TRequest request, CancellationToken cancellationToken = default(CancellationToken)) =>
        _pipeline.HandleAsync(request, cancellationToken);
}
