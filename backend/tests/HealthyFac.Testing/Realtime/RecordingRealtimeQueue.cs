using System.Collections.Concurrent;
using System.Diagnostics;
using HealthyFac.Shared.Application.BackgroundQueues;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Testing.Realtime;

/// <summary>
/// Replaces the real queue of <see cref="RealtimePush"/> in tests. It records each item with its parent context
/// and delivers nothing.
/// </summary>
public sealed class RecordingRealtimeQueue : IBackgroundQueue<RealtimePush>
{
    private readonly ConcurrentQueue<RecordedPush> _items = new();

    public IReadOnlyCollection<RecordedPush> Items => _items;

    public void Enqueue(RealtimePush item, ActivityContext parentContext) =>
        _items.Enqueue(new RecordedPush { Push = item, ParentContext = parentContext });

    public void Clear() => _items.Clear();
}

public sealed class RecordedPush
{
    public required RealtimePush Push { get; init; }

    public required ActivityContext ParentContext { get; init; }
}
