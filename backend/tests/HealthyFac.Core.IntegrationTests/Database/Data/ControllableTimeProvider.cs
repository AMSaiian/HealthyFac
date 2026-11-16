namespace HealthyFac.Core.IntegrationTests.Database.Data;

/// <summary>
/// Follows the system clock until a test freezes it or steps it. The fixture releases it before each test, so a test
/// that does neither sees the real time, like before.
/// </summary>
public sealed class ControllableTimeProvider : TimeProvider
{
    private DateTimeOffset? _frozenAt;
    private long _stepTicks;
    private long _nextTicks;

    public void Freeze(DateTimeOffset at)
    {
        _stepTicks = 0;
        _frozenAt = at;
    }

    /// <summary>
    /// Makes every read return a later instant than the read before it. A race test needs it, because the system
    /// clock can give two parallel calls the same instant, and the state history allows one row for each instant.
    /// </summary>
    /// <param name="from">The instant of the first read.</param>
    /// <param name="step">The time between two reads.</param>
    public void StepFrom(DateTimeOffset from, TimeSpan step)
    {
        _frozenAt = null;
        Interlocked.Exchange(ref _nextTicks, from.UtcTicks);
        _stepTicks = step.Ticks;
    }

    public void Release()
    {
        _frozenAt = null;
        _stepTicks = 0;
    }

    public override DateTimeOffset GetUtcNow()
    {
        if (_stepTicks != 0)
        {
            var ticks = Interlocked.Add(ref _nextTicks, _stepTicks) - _stepTicks;

            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        return _frozenAt ?? System.GetUtcNow();
    }
}
