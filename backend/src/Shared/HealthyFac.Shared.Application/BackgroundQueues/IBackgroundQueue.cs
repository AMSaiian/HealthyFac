using System.Diagnostics;

namespace HealthyFac.Shared.Application.BackgroundQueues;

/// <summary>
/// An in-process queue of items that a background sender handles after the request that added them has ended.
/// </summary>
/// <typeparam name="TItem">The type of the queued item. An item holds plain data only.</typeparam>
public interface IBackgroundQueue<in TItem>
{
    /// <summary>
    /// Adds an item and returns at once. The call never waits for the handler.
    /// </summary>
    /// <param name="item">The item to handle.</param>
    /// <param name="parentContext">The trace context of the operation that adds the item, so the handler span links
    /// to it.</param>
    void Enqueue(TItem item, ActivityContext parentContext);
}
