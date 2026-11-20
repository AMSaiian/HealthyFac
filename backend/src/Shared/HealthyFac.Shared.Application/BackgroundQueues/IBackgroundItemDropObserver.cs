namespace HealthyFac.Shared.Application.BackgroundQueues;

/// <summary>
/// Optional. Learns about each item that the queue discards without handling it: the oldest item of a full bounded
/// queue, an item that arrives after the stop, an item that the stop leaves unhandled.
/// </summary>
/// <typeparam name="TItem">The type of the dropped item.</typeparam>
public interface IBackgroundItemDropObserver<in TItem>
{
    void OnDropped(TItem item);
}
