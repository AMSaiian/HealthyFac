using System.Diagnostics;

namespace HealthyFac.Shared.Application.BackgroundQueues;

/// <summary>
/// Handles the items of one <see cref="IBackgroundQueue{TItem}"/>. Each item runs in its own DI scope, and each item
/// type has exactly one handler.
/// </summary>
/// <typeparam name="TItem">The type of the handled item.</typeparam>
public interface IBackgroundItemHandler<in TItem>
{
    /// <summary>
    /// Handles one item.
    /// </summary>
    /// <param name="item">The queued item.</param>
    /// <param name="parentContext">The trace context of the operation that added the item.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the item is handled.</returns>
    Task HandleAsync(TItem item, ActivityContext parentContext, CancellationToken cancellationToken);
}
