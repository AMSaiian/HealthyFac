using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Infrastructure.Caching;

/// <summary>
/// Skips Redis after a failure, so a request does not wait for a timeout while Redis is down. One singleton
/// serves every caller. The breaker has three states.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><c>Closed</c>: callers use Redis.</description></item>
/// <item><description><c>Open</c>: callers skip Redis for <see cref="RedisConstants.OpenPeriod"/>.</description></item>
/// <item><description><c>HalfOpen</c>: one caller tests Redis. The others keep skipping it.</description></item>
/// </list>
/// The first caller after the open period becomes the only probe. When the probe never reports, because the
/// request was cancelled, the next caller starts a new probe after <see cref="RedisConstants.ProbeTimeout"/>.
/// A successful probe closes the breaker. A failed probe opens it again for a full open period.
/// The breaker logs one warning when it opens and one when it closes. A failed probe logs at Debug only, so a long
/// outage logs one warning. The lock holds no I/O and no <c>await</c>, and the closed state needs no lock.
/// </remarks>
public sealed class RedisCircuitBreaker(TimeProvider timeProvider, ILogger<RedisCircuitBreaker> logger)
{
    private readonly Lock _gate = new();
    private volatile BreakerState _state = BreakerState.Closed;
    private DateTimeOffset _retryAt;

    private enum BreakerState
    {
        Closed,
        Open,
        HalfOpen,
    }

    /// <summary>
    /// Asks whether the caller may call Redis. A <c>true</c> answer in the open state turns the caller into the
    /// probe, so the caller must report the outcome with <see cref="RecordSuccess"/> or
    /// <see cref="RecordFailure"/>.
    /// </summary>
    /// <returns><c>true</c> when the caller may call Redis; <c>false</c> when the caller must skip it.</returns>
    public bool CanTryRedis()
    {
        if (_state == BreakerState.Closed)
        {
            return true;
        }

        lock (_gate)
        {
            if (_state == BreakerState.Closed)
            {
                return true;
            }

            var now = timeProvider.GetUtcNow();
            if (now < _retryAt)
            {
                return false;
            }

            _state = BreakerState.HalfOpen;
            _retryAt = now + RedisConstants.ProbeTimeout;

            return true;
        }
    }

    /// <summary>
    /// Reports a failed Redis call. The breaker opens for <see cref="RedisConstants.OpenPeriod"/>.
    /// </summary>
    /// <param name="exception">The failure of the Redis call.</param>
    public void RecordFailure(Exception exception)
    {
        BreakerState previousState;

        lock (_gate)
        {
            previousState = _state;
            if (previousState == BreakerState.Open)
            {
                return;
            }

            _state = BreakerState.Open;
            _retryAt = timeProvider.GetUtcNow() + RedisConstants.OpenPeriod;
        }

        if (previousState == BreakerState.Closed)
        {
            logger.LogWarning(
                "Redis circuit breaker opened for {OpenPeriod}. Failure: {FailureMessage}",
                RedisConstants.OpenPeriod,
                exception.Message);

            return;
        }

        logger.LogDebug(
            "Redis circuit breaker opened again after a failed probe. Failure: {FailureMessage}",
            exception.Message);
    }

    /// <summary>
    /// Reports a successful Redis call. Only the success of the probe closes the breaker. A late success from a
    /// request that started before the breaker opened changes nothing.
    /// </summary>
    public void RecordSuccess()
    {
        if (_state != BreakerState.HalfOpen)
        {
            return;
        }

        var isClosed = false;

        lock (_gate)
        {
            if (_state == BreakerState.HalfOpen)
            {
                _state = BreakerState.Closed;
                isClosed = true;
            }
        }

        if (isClosed)
        {
            logger.LogWarning("Redis circuit breaker closed after a successful probe. Redis is used again.");
        }
    }
}
