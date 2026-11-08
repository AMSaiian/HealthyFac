using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Delays the next save by the time that the test gives, so a test can stop the intake while a message is in flight.
/// The delay observes the cancellation token of the save.
/// </summary>
public sealed class SlowSaveInterceptor : SaveChangesInterceptor
{
    private long _delayTicks;
    private int _hasStarted;

    public bool HasStarted => Volatile.Read(ref _hasStarted) == 1;

    public void Reset()
    {
        Interlocked.Exchange(ref _hasStarted, value: 0);
        Interlocked.Exchange(ref _delayTicks, value: 0);
    }

    public void Arm(TimeSpan delay)
    {
        Interlocked.Exchange(ref _hasStarted, value: 0);
        Interlocked.Exchange(ref _delayTicks, delay.Ticks);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var delayTicks = Interlocked.Exchange(ref _delayTicks, value: 0);

        if (delayTicks > 0)
        {
            Interlocked.Exchange(ref _hasStarted, value: 1);
            await Task.Delay(TimeSpan.FromTicks(delayTicks), cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
