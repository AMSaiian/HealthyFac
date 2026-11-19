using HealthyFac.Telemetry.Domain.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;

/// <summary>
/// Makes the save of the next machine run fail once. The save of a run is the one that changes a machine evaluation
/// without a lease, so the save of the claim, which sets the lease, passes. A test uses it to check what the worker
/// does with a failed run.
/// </summary>
public sealed class FailRunSaveInterceptor : SaveChangesInterceptor
{
    public const string FailureMessage = "The run save failed on purpose.";

    private Exception? _failure;
    private Action? _beforeFailure;

    public void Arm(Exception? failure = null, Action? beforeFailure = null)
    {
        _beforeFailure = beforeFailure;
        Interlocked.Exchange(ref _failure, failure ?? new InvalidOperationException(FailureMessage));
    }

    public void Reset() => Interlocked.Exchange(ref _failure, value: null);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var isRunSave = eventData.Context!.ChangeTracker.Entries<MachineEvaluation>()
            .Any(entry => entry.State == EntityState.Modified && entry.Entity.LeaseUntil is null);

        if (isRunSave && Interlocked.Exchange(ref _failure, value: null) is { } failure)
        {
            _beforeFailure?.Invoke();

            throw failure;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
