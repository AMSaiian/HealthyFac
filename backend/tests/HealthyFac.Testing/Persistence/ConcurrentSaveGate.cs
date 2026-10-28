using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthyFac.Testing.Persistence;

/// <summary>
/// Makes parallel saves overlap. <c>Task.WhenAll</c> only starts the calls, so a call can finish its save before
/// the next call reads its data, and then the database guard (a row version or a unique index) never runs. The
/// gate holds the first save of every participant of a race until all participants have reached their first
/// save, so every call has read its data before any call writes.
/// </summary>
/// <remarks>
/// A save that belongs to no participant passes at once. This covers the arrange step of a test and the saves of
/// other tests that share the fixture. Register one instance as an interceptor of the context under test.
/// </remarks>
public sealed class ConcurrentSaveGate : SaveChangesInterceptor
{
    private const int DefaultTimeoutSeconds = 10;

    private readonly TimeSpan _timeout;
    private readonly AsyncLocal<(ConcurrentSaveRace Race, int Index)?> _participant = new();

    /// <summary>
    /// Creates a gate with the default time limit of a wait.
    /// </summary>
    public ConcurrentSaveGate()
        : this(TimeSpan.FromSeconds(DefaultTimeoutSeconds))
    {
    }

    /// <summary>
    /// Creates a gate with its own time limit of a wait.
    /// </summary>
    /// <param name="timeout">The longest time that a save waits for the other participants.</param>
    public ConcurrentSaveGate(TimeSpan timeout)
    {
        _timeout = timeout;
    }

    /// <summary>
    /// Starts a race. Run each participant with <see cref="ConcurrentSaveRace.RunAsync"/>.
    /// </summary>
    /// <param name="participantCount">The number of racing calls.</param>
    /// <returns>The race, which the test disposes when it ends.</returns>
    public ConcurrentSaveRace StartRace(int participantCount) => new(this, participantCount, _timeout);

    /// <summary>
    /// Holds the first save of a participant until all participants have reached their first save. Later saves
    /// of a participant, and saves of a flow that is no participant, pass at once.
    /// </summary>
    /// <param name="eventData">The context data of the save.</param>
    /// <param name="result">The interception result of the save.</param>
    /// <param name="cancellationToken">The token that stops the wait.</param>
    /// <returns>The unchanged interception result.</returns>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        var participant = _participant.Value;
        if (participant is not null)
        {
            await participant.Value.Race.ArriveAsync(participant.Value.Index, cancellationToken);
        }

        return result;
    }

    internal void Enter(ConcurrentSaveRace race, int index) => _participant.Value = (race, index);
}
