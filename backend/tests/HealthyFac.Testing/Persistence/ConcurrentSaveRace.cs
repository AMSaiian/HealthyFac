using HealthyFac.Testing.Exceptions;

namespace HealthyFac.Testing.Persistence;

/// <summary>
/// A set of calls that race to save. Each call runs through <see cref="RunAsync"/>, which gives it its own
/// participant ID. The race releases the first saves of all participants at once.
/// </summary>
public sealed class ConcurrentSaveRace : IDisposable
{
    private readonly ConcurrentSaveGate _gate;
    private readonly int _participantCount;
    private readonly TimeSpan _timeout;
    private readonly int[] _saveCounts;
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _startedCount;
    private int _arrivedCount;

    internal ConcurrentSaveRace(ConcurrentSaveGate gate, int participantCount, TimeSpan timeout)
    {
        _gate = gate;
        _participantCount = participantCount;
        _timeout = timeout;
        _saveCounts = new int[participantCount];
    }

    /// <summary>
    /// The number of saves of each participant, in the order that the calls started. A test asserts that each
    /// participant saved once, because the gate holds the first save: a handler that gains an earlier save would
    /// make the gate hold the wrong one.
    /// </summary>
    public IReadOnlyList<int> SaveCounts => _saveCounts.ToList();

    /// <summary>
    /// Runs one participant on its own flow, so its saves carry its participant ID.
    /// </summary>
    /// <param name="call">The call of the participant.</param>
    /// <returns>The exception of the call, or <see langword="null"/> when the call succeeds.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the number of started participants would exceed the participant count of the race.
    /// </exception>
    public Task<Exception?> RunAsync(Func<Task> call)
    {
        var index = Interlocked.Increment(ref _startedCount) - 1;
        if (index >= _participantCount)
        {
            throw ConcurrentSaveGateErrors.TooManyParticipants(_participantCount);
        }

        return Task.Run(() =>
        {
            _gate.Enter(this, index);

            return ExceptionCapture.CaptureAsync(call);
        });
    }

    /// <summary>
    /// Frees every save that still waits, so a finished test leaves no hanging call.
    /// </summary>
    public void Dispose() => _released.TrySetResult();

    /// <summary>
    /// Records a save of a participant. The first save waits until all participants have reached their first
    /// save.
    /// </summary>
    /// <param name="index">The participant ID.</param>
    /// <param name="cancellationToken">The token that stops the wait.</param>
    /// <exception cref="TimeoutException">
    /// Thrown when not all participants reach their first save within the time limit, for example when a call
    /// fails before its save.
    /// </exception>
    internal async Task ArriveAsync(int index, CancellationToken cancellationToken)
    {
        var saveCount = Interlocked.Increment(ref _saveCounts[index]);
        if (saveCount > 1)
        {
            return;
        }

        var arrivedCount = Interlocked.Increment(ref _arrivedCount);
        if (arrivedCount >= _participantCount)
        {
            _released.TrySetResult();

            return;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);

        try
        {
            await _released.Task.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw ConcurrentSaveGateErrors.Timeout(Volatile.Read(ref _arrivedCount), _participantCount);
        }
    }
}

internal static class ConcurrentSaveGateErrors
{
    public static TimeoutException Timeout(int arrivedCount, int participantCount) =>
        new(
            $"Only {arrivedCount} of {participantCount} participants reached their save. "
            + "A call probably failed before its save.");

    public static InvalidOperationException TooManyParticipants(int participantCount) =>
        new($"The race has {participantCount} participants, and all of them have started.");
}
