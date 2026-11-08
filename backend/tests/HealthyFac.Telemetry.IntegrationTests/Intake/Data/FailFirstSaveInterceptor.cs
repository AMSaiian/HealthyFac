using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Makes the next save fail once with the exception that the test gives, so a test can check what the intake does
/// with a transient and with a non-transient failure. It counts every save that it sees.
/// </summary>
public sealed class FailFirstSaveInterceptor : SaveChangesInterceptor
{
    private Exception? _failure;
    private int _saveCount;

    public int SaveCount => _saveCount;

    public void Arm(Exception failure) => Interlocked.Exchange(ref _failure, failure);

    public void Reset() => Interlocked.Exchange(ref _failure, value: null);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Interlocked.Increment(ref _saveCount);

        var failure = Interlocked.Exchange(ref _failure, value: null);

        if (failure is not null)
        {
            throw failure;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
