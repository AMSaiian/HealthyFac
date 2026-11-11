namespace HealthyFac.Insights.UnitTests.Conversion.Data;

/// <summary>
/// A time provider whose timers fire at once, so a test does not wait for a delay. It records the requested delays.
/// </summary>
public sealed class ImmediateTimeProvider : TimeProvider
{
    private readonly List<TimeSpan> _delays = [];

    public IReadOnlyList<TimeSpan> Delays => _delays;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        _delays.Add(dueTime);
        ThreadPool.QueueUserWorkItem(_ => callback(state));

        return new ImmediateTimer();
    }

    private sealed class ImmediateTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
