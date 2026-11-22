namespace HealthyFac.Seeder.Timeline;

/// <summary>
/// The clock of the seed job. The services read it as their <see cref="TimeProvider"/>, so every stored time follows
/// the moment of the seed event. Reads and <see cref="Enter"/> are thread-safe.
/// </summary>
/// <remarks>
/// Each read returns 1 µs more than the read before, so no two reads return the same value: stored rows
/// then keep a strict order. The local time zone is UTC. A read before the first <see cref="Enter"/> throws, so no
/// row gets a time that no event gave.
/// </remarks>
public sealed class SeedClock : TimeProvider
{
    private static readonly TimeSpan ReadStep = TimeSpan.FromMicroseconds(microseconds: 1);

    private readonly Lock _lock = new();
    private DateTimeOffset? _lastRead;
    private DateTimeOffset? _enteredMoment;
    private DateTimeOffset? _previousMoment;

    /// <summary>
    /// Starts an event. A moment later than the previous one starts at that moment. A moment equal to the previous
    /// one continues after the last read (the next read is 1 µs more), so events with the same moment run in file
    /// order and the times stay strictly increasing.
    /// </summary>
    /// <param name="moment">The moment of the event.</param>
    /// <param name="item">The seed item of the event, named in the error.</param>
    /// <exception cref="InvalidOperationException">
    /// The <paramref name="moment"/> is earlier than the previous moment, or not later than the last read.
    /// </exception>
    public void Enter(DateTimeOffset moment, string item)
    {
        lock (_lock)
        {
            if (_previousMoment is { } previous && moment < previous)
            {
                throw new InvalidOperationException(TimelineErrors.ClockWouldGoBack(item, moment, previous));
            }

            if (_previousMoment != moment)
            {
                if (_lastRead is { } lastRead && moment <= lastRead)
                {
                    throw new InvalidOperationException(TimelineErrors.ClockWouldGoBack(item, moment, lastRead));
                }

                _enteredMoment = moment;
            }

            _previousMoment = moment;
        }
    }

    /// <exception cref="InvalidOperationException">No event was entered before the read.</exception>
    public override DateTimeOffset GetUtcNow()
    {
        lock (_lock)
        {
            if (_enteredMoment is null && _lastRead is null)
            {
                throw new InvalidOperationException(TimelineErrors.ClockNotEntered);
            }

            var read = _enteredMoment ?? _lastRead!.Value + ReadStep;
            _enteredMoment = null;
            _lastRead = read;

            return read;
        }
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
