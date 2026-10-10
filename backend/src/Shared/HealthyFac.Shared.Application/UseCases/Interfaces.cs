namespace HealthyFac.Shared.Application.UseCases;

/// <summary>
/// Marks a use case request that returns no response.
/// </summary>
public interface IRequest;

/// <summary>
/// Marks a use case request that returns a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response that the handler returns.</typeparam>
public interface IRequest<out TResponse>;

/// <summary>
/// Handles one use case request that returns no response. Decorators wrap the handler for validation and logging.
/// </summary>
/// <typeparam name="TRequest">The type of the request that the handler accepts.</typeparam>
public interface IRequestHandler<in TRequest> where TRequest : IRequest
{
    Task HandleAsync(TRequest request, CancellationToken ct = default);
}

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
