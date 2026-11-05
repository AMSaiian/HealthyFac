using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Shared.Application.UseCases.Decorators;

/// <summary>
/// Decorates a request handler to run the pre-handler check of the request after the input validation and before
/// the handler.
/// </summary>
internal sealed class PreHandlerCheckRequestHandlerDecorator<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> innerHandler,
    IPreHandlerCheck<TRequest> preHandlerCheck
) : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IRequestHandler<TRequest, TResponse> _innerHandler = innerHandler;
    private readonly IPreHandlerCheck<TRequest> _preHandlerCheck = preHandlerCheck;

    /// <summary>
    /// Runs the pre-handler check, then executes the inner handler.
    /// </summary>
    /// <param name="request">The incoming request to check and execute.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>The response of type <typeparamref name="TResponse"/> from the inner handler.</returns>
    public async Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        await RunCheckAsync(request, cancellationToken);

        return await _innerHandler.HandleAsync(request, cancellationToken);
    }

    /// <summary>
    /// Runs the pre-handler check inside its own span. The span ends before the inner handler starts, so the handler span
    /// and the check span stay siblings.
    /// </summary>
    /// <param name="request">The incoming request to check.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    private async Task RunCheckAsync(TRequest request, CancellationToken cancellationToken)
    {
        using var activity = UseCaseActivitySource.Instance.StartActivity(ObservabilityConstants.Spans.PreHandlerCheck);
        activity.WithTag(ObservabilityConstants.Tags.UseCase, typeof(TRequest).Name);

        try
        {
            await _preHandlerCheck.CheckAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is ForbiddenAccessException or NotFoundException)
        {
            activity.MarkRejected();

            throw;
        }

        activity.MarkSuccess();
    }
}
