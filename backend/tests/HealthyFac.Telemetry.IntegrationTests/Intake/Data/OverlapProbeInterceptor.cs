using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Holds the first save after <see cref="Arm"/> until a second save starts, or until the hold limit passes. It
/// records whether two saves ran at the same time, so a test can prove that two consumers work in parallel.
/// </summary>
public sealed class OverlapProbeInterceptor : SaveChangesInterceptor
{
    private static readonly TimeSpan HoldLimit = TimeSpan.FromSeconds(seconds: 10);

    private TaskCompletionSource _secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _isArmed;
    private int _startedCount;
    private int _hasOverlapped;

    public bool HasOverlapped => Volatile.Read(ref _hasOverlapped) == 1;

    public void Arm() => Interlocked.Exchange(ref _isArmed, value: 1);

    public void Reset()
    {
        Interlocked.Exchange(ref _isArmed, value: 0);
        Interlocked.Exchange(ref _startedCount, value: 0);
        Interlocked.Exchange(ref _hasOverlapped, value: 0);
        Interlocked.Exchange(
            ref _secondStarted, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (Volatile.Read(ref _isArmed) == 1)
        {
            var startedNumber = Interlocked.Increment(ref _startedCount);

            if (startedNumber == 1)
            {
                await Task.WhenAny(_secondStarted.Task, Task.Delay(HoldLimit, cancellationToken));
                Interlocked.Exchange(ref _hasOverlapped, _secondStarted.Task.IsCompleted ? 1 : 0);
            }
            else if (startedNumber == 2)
            {
                _secondStarted.TrySetResult();
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
