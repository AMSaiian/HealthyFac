using System.Diagnostics;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Observability;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Application.UseCases.Decorators;

/// <summary>
/// Decorates a request handler to trace and log each use case execution.
/// The activity owns the duration, so the decorator measures no elapsed time itself. It logs one
/// information entry per use case, and the payloads only at debug level.
/// </summary>
internal sealed class LoggingRequestHandlerDecorator<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> innerHandler,
    ILogger<LoggingRequestHandlerDecorator<TRequest, TResponse>> logger
) : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Traces and logs the use case, then executes the inner handler.
    /// </summary>
    /// <param name="request">The incoming request to execute.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>The response of type <typeparamref name="TResponse"/> from the inner handler.</returns>
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken ct = default)
    {
        var useCaseName = typeof(TRequest).Name;

        using var activity = UseCaseActivitySource.Instance.StartActivity(useCaseName, ActivityKind.Internal);
        activity.WithTag(ObservabilityConstants.Tags.UseCase, useCaseName);

        logger.LogInformation("Use case {UseCase} triggered", useCaseName);
        logger.LogDebug("Use case {UseCase} received request {@Request}", useCaseName, request);

        try
        {
            var response = await innerHandler.HandleAsync(request, ct);

            activity.MarkSuccess();
            logger.LogDebug("Use case {UseCase} returned {@Response}", useCaseName, response);
            logger.LogInformation("Use case {UseCase} finished", useCaseName);

            return response;
        }
        catch (Exception exception)
        {
            activity.MarkException(exception);
            LogOutcome(exception, useCaseName);

            throw;
        }
    }

    /// <summary>
    /// Logs why the use case ended with an exception. A rejection is handled, so it logs one line without a
    /// stack trace at the level that the exception declares, and a null level logs nothing. Any other
    /// exception logs one debug line without the exception, because the exception handler already logs the
    /// exception with its stack trace.
    /// </summary>
    /// <param name="exception">The exception that ended the use case.</param>
    /// <param name="useCaseName">The name of the use case.</param>
    private void LogOutcome(Exception exception, string useCaseName)
    {
        if (exception is IRejection rejection)
        {
            if (rejection.Level is not null)
            {
                logger.Log(
                    rejection.Level.Value,
                    "Use case {UseCase} rejected: {Reason}", useCaseName, exception.Message);
            }
        }
        else
        {
            logger.LogDebug("Use case {UseCase} failed", useCaseName);
        }
    }
}
