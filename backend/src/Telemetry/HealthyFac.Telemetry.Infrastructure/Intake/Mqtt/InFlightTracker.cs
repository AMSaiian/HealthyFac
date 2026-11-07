namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// Counts the messages that a consumer is handling and remembers the last moment of activity. A stopping consumer
/// closes it, so a message that arrives after the stop signal is not started, and then waits until the count is 0.
/// </summary>
/// <param name="timeProvider">Provides the moments of activity.</param>
internal sealed class InFlightTracker(TimeProvider timeProvider)
{
    private readonly TimeProvider _timeProvider = timeProvider;
    private int _count;
    private volatile bool _isClosed;
    private long _lastActivityTimestamp = timeProvider.GetTimestamp();

    public bool IsEmpty => Volatile.Read(ref _count) == 0;

    /// <summary>
    /// Counts a message as started, unless the tracker is closed.
    /// </summary>
    /// <returns><see langword="true"/> when the caller may handle the message and must call <see cref="End"/>;
    /// <see langword="false"/> when the tracker is closed and the caller must not acknowledge the message.</returns>
    public bool TryBegin()
    {
        Interlocked.Increment(ref _count);

        if (_isClosed)
        {
            Interlocked.Decrement(ref _count);

            return false;
        }

        Touch();

        return true;
    }

    public void End()
    {
        Touch();
        Interlocked.Decrement(ref _count);
    }

    public void Close() => _isClosed = true;

    public void Touch() => Interlocked.Exchange(ref _lastActivityTimestamp, _timeProvider.GetTimestamp());

    public bool IsIdleFor(TimeSpan idleTime) =>
        IsEmpty && _timeProvider.GetElapsedTime(Volatile.Read(ref _lastActivityTimestamp)) >= idleTime;
}
