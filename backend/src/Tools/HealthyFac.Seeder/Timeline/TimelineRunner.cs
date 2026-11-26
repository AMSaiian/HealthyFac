using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.Timeline;

/// <summary>
/// Runs the seed events in the order of their moments. Before the first event, it enters the initialization moment
/// (1 minute before the earliest event), initializes the databases, and relays their messages. For each event it
/// enters the moment into the seed clock, lets the part run the event, and then relays the messages that the event
/// caused.
/// </summary>
public sealed class TimelineRunner
{
    public const string InitializationItem = "initialization";

    private static readonly TimeSpan InitializationLead = TimeSpan.FromMinutes(minutes: 1);

    private readonly SeedClock _clock;
    private readonly DateTimeOffset _anchor;
    private readonly TimeZoneInfo _zone;
    private readonly Func<CancellationToken, Task> _initializeAsync;
    private readonly Func<string, CancellationToken, Task> _relayAsync;
    private readonly Action<string> _log;
    private readonly Func<CancellationToken, Task<bool>>? _continueAfterInitializeAsync;

    /// <summary>Creates a runner.</summary>
    /// <param name="clock">The clock of the service hosts.</param>
    /// <param name="anchor">The start of the job.</param>
    /// <param name="zone">The company zone.</param>
    /// <param name="initializeAsync">Initializes the 5 databases (the migrations and the required data).</param>
    /// <param name="relayAsync">Relays the pending messages. The argument is the item of the event.</param>
    /// <param name="log">Writes one line of the log. A line holds event items, never a password or a token.</param>
    /// <param name="continueAfterInitializeAsync">Runs after the initialization. A result of <see langword="false"/>
    /// ends the run before the first event.</param>
    public TimelineRunner(
        SeedClock clock,
        DateTimeOffset anchor,
        TimeZoneInfo zone,
        Func<CancellationToken, Task> initializeAsync,
        Func<string, CancellationToken, Task> relayAsync,
        Action<string> log,
        Func<CancellationToken, Task<bool>>? continueAfterInitializeAsync = null
    )
    {
        _clock = clock;
        _anchor = anchor;
        _zone = zone;
        _initializeAsync = initializeAsync;
        _relayAsync = relayAsync;
        _log = log;
        _continueAfterInitializeAsync = continueAfterInitializeAsync;
    }

    /// <summary>Runs the events.</summary>
    /// <param name="events">
    /// The events in file order. Events with the same moment keep this order: the clock continues after the last read
    /// of the event before (1 µs more), so no stored time repeats.
    /// </param>
    /// <param name="context">The keys and the actors of the job.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation requests. It is
    /// checked before each event, never inside one.</param>
    /// <returns>A task that completes after the last event and its messages.</returns>
    /// <exception cref="InvalidOperationException">An event failed. The message names its item.</exception>
    public async Task RunAsync(
        IReadOnlyList<PlannedEvent> events,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var ordered = events
            .Select(planned => (Planned: planned, At: SeedCalendar.ToUtc(planned.Event.At, _anchor, _zone)))
            .OrderBy(entry => entry.At)
            .ToList();

        await InitializeAsync(ordered.Count > 0 ? ordered[0].At : _anchor, cancellationToken);

        if (_continueAfterInitializeAsync is not null && !await _continueAfterInitializeAsync(cancellationToken))
        {
            return;
        }

        foreach (var (planned, at) in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await RunEventAsync(planned, at, context, cancellationToken);
        }
    }

    private async Task InitializeAsync(DateTimeOffset earliest, CancellationToken cancellationToken)
    {
        _log(InitializationItem);

        try
        {
            _clock.Enter(earliest - InitializationLead, InitializationItem);
            await _initializeAsync(cancellationToken);
            await _relayAsync(InitializationItem, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                TimelineErrors.EventFailed(InitializationItem, exception.Message),
                exception);
        }
    }

    private async Task RunEventAsync(
        PlannedEvent planned,
        DateTimeOffset at,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var item = planned.Event.Item;
        _log(item);

        try
        {
            _clock.Enter(at, item);
            await planned.Part.RunAsync(planned.Event, context, cancellationToken);
            await _relayAsync(item, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(TimelineErrors.EventFailed(item, exception.Message), exception);
        }
    }
}
