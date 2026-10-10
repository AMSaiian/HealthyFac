using FluentValidation;
using FluentValidation.Results;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Observability;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Application.UseCases.Decorators;

/// <summary>
/// Decorates a request handler to validate incoming requests prior to handler execution.
/// Entry and exit are logged at trace level, because they describe pipeline mechanics rather than a
/// business event.
/// </summary>
public class ValidationRequestHandlerDecorator<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> innerHandler,
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationRequestHandlerDecorator<TRequest, TResponse>> logger
) : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Validates the request, then executes the inner handler.
    /// </summary>
    /// <param name="request">The incoming request to validate and execute.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>The response of type <typeparamref name="TResponse"/> from the inner handler.</returns>
    /// <exception cref="AppValidationException">
    /// Thrown when one or more validators report a failure for <paramref name="request"/>.
    /// </exception>
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken ct = default)
    {
        await ValidateAsync(request, ct);

        return await innerHandler.HandleAsync(request, ct);
    }

    /// <summary>
    /// Runs every validator inside its own span. The span ends before the inner handler starts, so the
    /// handler span and the validation span stay siblings and each shows its own duration.
    /// </summary>
    /// <param name="request">The incoming request to validate.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <exception cref="AppValidationException">
    /// Thrown when one or more validators report a failure for <paramref name="request"/>.
    /// </exception>
    private async Task ValidateAsync(TRequest request, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var applicableValidators = validators.ToList();

        using var activity = UseCaseActivitySource.Instance.StartActivity(ObservabilityConstants.Spans.Validation);
        activity
            .WithTag(ObservabilityConstants.Tags.UseCase, requestName)
            .WithTag(ObservabilityConstants.Tags.ValidatorCount, applicableValidators.Count);

        logger.LogTrace(
            "Validation entered for {RequestName} with {ValidatorCount} validator(s)",
            requestName,
            applicableValidators.Count);

        if (applicableValidators.Count != 0)
        {
            var validationResults = await Task.WhenAll(
                applicableValidators.Select(validator => validator.ValidateAsync(request, ct))
            );

            var failures = validationResults
                .SelectMany(result => result.Errors)
                .ToList();

            activity.WithTag(ObservabilityConstants.Tags.RejectionCount, failures.Count);

            if (failures.Count != 0)
            {
                logger.LogInformation(
                    "Validation rejected {RequestName}: {@Errors}",
                    requestName,
                    new ValidationResult(failures).ToDictionary());

                activity.MarkRejected();

                throw new AppValidationException(failures);
            }
        }

        activity.MarkSuccess();
        logger.LogTrace("Validation exited for {RequestName} with no failures", requestName);
    }
}
