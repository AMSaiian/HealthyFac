using System.Collections.Concurrent;
using System.Diagnostics;

namespace HealthyFac.Testing.Tracing;

/// <summary>
/// Records every finished span of the given sources. The listener is global, so a test filters the spans by the
/// trace ID of its own parent activity or by a tag, because other tests run in parallel.
/// </summary>
public sealed class SpanCapture : IDisposable
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(100);

    private readonly ConcurrentQueue<Activity> _spans = new();
    private readonly ActivityListener _listener;

    public SpanCapture(params string[] sourceNames)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => sourceNames.Contains(source.Name),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _spans.Enqueue,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public IReadOnlyList<Activity> Spans => _spans.ToList();

    /// <summary>
    /// Waits until at least <paramref name="expectedCount"/> finished spans match the filter, or until the
    /// timeout ends.
    /// </summary>
    public async Task<IReadOnlyList<Activity>> WaitForSpansAsync(Func<Activity, bool> filter, int expectedCount)
    {
        using var cancellation = new CancellationTokenSource(WaitTimeout);
        while (!cancellation.IsCancellationRequested && Spans.Count(filter) < expectedCount)
        {
            await Task.Delay(PollDelay);
        }

        return Spans.Where(filter).ToList();
    }

    public void Dispose() => _listener.Dispose();
}
