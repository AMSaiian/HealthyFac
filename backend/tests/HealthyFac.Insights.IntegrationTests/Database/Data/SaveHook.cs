using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthyFac.Insights.IntegrationTests.Database.Data;

/// <summary>
/// Runs a test action before the next save of any context, once. A test uses it to hold a save that already owns a row
/// lock, while another call tries to take the same lock. <see cref="ConcurrentSaveGate"/> cannot do this: it waits
/// until every participant reaches its save, and a call that waits for the lock never reaches it.
/// </summary>
public sealed class SaveHook : SaveChangesInterceptor
{
    private Func<Task>? _next;

    public void RunBeforeNextSave(Func<Task> action) => Interlocked.Exchange(ref _next, action);

    public void Clear() => Interlocked.Exchange(ref _next, null);

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var action = Interlocked.Exchange(ref _next, null);
        if (action is not null)
        {
            await action();
        }

        return result;
    }
}
