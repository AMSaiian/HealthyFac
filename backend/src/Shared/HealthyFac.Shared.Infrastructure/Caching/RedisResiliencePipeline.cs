using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;

namespace HealthyFac.Shared.Infrastructure.Caching;

/// <summary>
/// Runs every Redis call of the service through the shared Redis pipeline of <c>AddSharedRedis</c>, so one circuit
/// breaker sees every Redis failure. While the breaker is open, a call throws <see cref="BrokenCircuitException"/>
/// at once and never waits for a Redis timeout. The call sites and the health check use this type, so none of them
/// needs the pipeline key.
/// </summary>
/// <param name="pipeline">The shared Redis pipeline. <c>AddResiliencePipeline</c> registers it as a keyed service
/// under its pipeline key.</param>
/// <param name="stateProvider">Reads the state of the breaker of the shared pipeline.</param>
public sealed class RedisResiliencePipeline(
    [FromKeyedServices(RedisConstants.Resilience.PipelineKey)] ResiliencePipeline pipeline,
    [FromKeyedServices(RedisConstants.Resilience.PipelineKey)]
    CircuitBreakerStateProvider stateProvider
)
{
    private readonly ResiliencePipeline _pipeline = pipeline;
    private readonly CircuitBreakerStateProvider _stateProvider = stateProvider;

    public CircuitState State => _stateProvider.CircuitState;

    /// <summary>
    /// Runs one Redis call through the breaker. A <c>RedisException</c> or a <c>RedisTimeoutException</c> counts as
    /// a failure. Any other result, a cancellation included, counts as a success.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the call.</typeparam>
    /// <param name="operation">The Redis call. It gets the token of the caller.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The result of the call.</returns>
    /// <exception cref="BrokenCircuitException">Thrown when the breaker is open, half-open with a running probe, or
    /// isolated. The call then does not reach Redis.</exception>
    public ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken
    ) => _pipeline.ExecuteAsync(operation, cancellationToken);

    /// <summary>
    /// Runs one Redis call without a result through the breaker. A <c>RedisException</c> or a
    /// <c>RedisTimeoutException</c> counts as a failure. Any other result, a cancellation included, counts as a
    /// success.
    /// </summary>
    /// <param name="operation">The Redis call. It gets the token of the caller.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the call completes.</returns>
    /// <exception cref="BrokenCircuitException">Thrown when the breaker is open, half-open with a running probe, or
    /// isolated. The call then does not reach Redis.</exception>
    public ValueTask ExecuteAsync(Func<CancellationToken, ValueTask> operation, CancellationToken cancellationToken)
        => _pipeline.ExecuteAsync(operation, cancellationToken);
}
