using System.Diagnostics;
using System.Threading.Channels;
using HealthyFac.Shared.Application.BackgroundQueues;

namespace HealthyFac.Shared.Infrastructure.BackgroundQueues;

/// <summary>
/// The singleton queue of one item type. <see cref="Enqueue"/> never waits. A bounded queue drops its oldest item
/// when it is full.
/// </summary>
/// <typeparam name="TItem">The type of the queued item.</typeparam>
public sealed class BackgroundQueue<TItem> : IBackgroundQueue<TItem>
{
    private readonly Channel<BackgroundEnvelope<TItem>> _channel;
    private readonly IBackgroundItemDropObserver<TItem>? _dropObserver;

    public BackgroundQueue(BackgroundQueueCapacity capacity, IBackgroundItemDropObserver<TItem>? dropObserver = null)
    {
        _dropObserver = dropObserver;

        _channel = capacity.Limit is { } limit
            ? Channel.CreateBounded<BackgroundEnvelope<TItem>>(
                new BoundedChannelOptions(limit) { FullMode = BoundedChannelFullMode.DropOldest },
                envelope => ReportDropped(envelope.Item))
            : Channel.CreateUnbounded<BackgroundEnvelope<TItem>>(new UnboundedChannelOptions());
    }

    /// <summary>The number of items that wait for the handler. The queue-length gauge reads it.</summary>
    public int Count => _channel.Reader.Count;

    public void Enqueue(TItem item, ActivityContext parentContext)
    {
        var envelope = new BackgroundEnvelope<TItem> { Item = item, ParentContext = parentContext };

        if (!_channel.Writer.TryWrite(envelope))
        {
            ReportDropped(item);
        }
    }

    internal IAsyncEnumerable<BackgroundEnvelope<TItem>> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    internal void Complete() => _channel.Writer.TryComplete();

    internal void DropRemaining()
    {
        while (_channel.Reader.TryRead(out var envelope))
        {
            ReportDropped(envelope.Item);
        }
    }

    internal void ReportDropped(TItem item) => _dropObserver?.OnDropped(item);
}
