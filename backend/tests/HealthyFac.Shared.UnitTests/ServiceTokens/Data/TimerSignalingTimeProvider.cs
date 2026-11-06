using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// A fake clock that tells a test when the code under test has created a timer. A call that waits with
/// <c>Task.Delay</c> creates its timer some time after it starts. A test that moves the clock before that moment
/// would miss the timer, so the test waits for the timer first.
/// </summary>
public sealed class TimerSignalingTimeProvider : FakeTimeProvider
{
    private readonly SemaphoreSlim _created = new(initialCount: 0);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        _created.Release();

        return timer;
    }

    /// <summary>
    /// Waits until the code under test has created more timers.
    /// </summary>
    /// <param name="count">The number of new timers to wait for.</param>
    /// <returns>A task that completes when the timers exist.</returns>
    public async Task WaitForTimersAsync(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await _created.WaitAsync();
        }
    }
}
