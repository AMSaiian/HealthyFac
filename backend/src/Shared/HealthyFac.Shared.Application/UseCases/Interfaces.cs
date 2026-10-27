namespace HealthyFac.Shared.Application.UseCases;

/// <summary>
/// The response of a use case without a result. Only the shared use case code uses it: an implementer derives from
/// <see cref="RequestHandler{TRequest}"/>, and a caller injects <see cref="IRequestHandler{TRequest}"/>. It must
/// stay public, because the public <see cref="IRequest"/> and <see cref="RequestHandler{TRequest}"/> expose it.
/// </summary>
public readonly record struct Unit;

/// <summary>
/// Marks a use case request that returns no response.
/// </summary>
public interface IRequest : IRequest<Unit>;

/// <summary>
/// Marks a use case request that returns a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response that the handler returns.</typeparam>
public interface IRequest<out TResponse>;

/// <summary>
/// Handles one use case request that returns a response. Decorators wrap the handler for validation and
/// logging.
/// </summary>
/// <typeparam name="TRequest">The type of the request that the handler accepts.</typeparam>
/// <typeparam name="TResponse">The type of the response that the handler returns.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken ct = default);
}

/// <summary>
/// For callers of a use case without a result. Only the shared adapter implements it. The adapter calls the
/// decorated <see cref="IRequestHandler{TRequest,TResponse}"/> with <see cref="Unit"/>, so validation and logging
/// always run.
/// </summary>
/// <typeparam name="TRequest">The type of the request that the handler accepts.</typeparam>
public interface IRequestHandler<in TRequest> where TRequest : IRequest
{
    Task HandleAsync(TRequest request, CancellationToken ct = default);
}
