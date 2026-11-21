using HealthyFac.Shared.Application.BackgroundQueues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Infrastructure.BackgroundQueues;

/// <summary>
/// Reads the queue of one item type and runs each item through its handler in a new DI scope. A failing handler
/// logs a warning, and the loop goes on with the next item.
/// </summary>
/// <remarks>
/// The stop has two phases. When the host starts the stop, the queue accepts no more items, and the sender keeps
/// handling the queued items. When the token of <see cref="StopAsync"/> fires, the sender cancels the item in work
/// and reports the item and every item that is left as dropped, and <see cref="StopAsync"/> returns after the
/// loop has counted them. The sender is not a <c>BackgroundService</c>,
/// because that base class cancels its own token at the stop and a drain needs its own limit.
/// </remarks>
/// <typeparam name="TItem">The type of the handled item.</typeparam>
/// <param name="queue">The queue to read.</param>
/// <param name="scopeFactory">Creates the scope of each item.</param>
/// <param name="logger">The logger of the sender.</param>
internal sealed class BackgroundQueueSender<TItem>(
    BackgroundQueue<TItem> queue,
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundQueueSender<TItem>> logger
) : IHostedService, IDisposable
{
    private readonly BackgroundQueue<TItem> _queue = queue;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<BackgroundQueueSender<TItem>> _logger = logger;
    private readonly CancellationTokenSource _drainLimit = new();

    private Task _loop = Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _loop = Task.Run(RunAsync, CancellationToken.None);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Complete();

        try
        {
            await _loop.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await _drainLimit.CancelAsync();
            await _loop.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    public void Dispose() => _drainLimit.Dispose();

    private async Task RunAsync()
    {
        try
        {
            await foreach (var envelope in _queue.ReadAllAsync(_drainLimit.Token))
            {
                await HandleAsync(envelope, _drainLimit.Token);
            }
        }
        catch (OperationCanceledException) when (_drainLimit.IsCancellationRequested)
        {
            _logger.LogDebug(
                message: "The drain limit of the background queue of {ItemType} ended.", typeof(TItem).Name);
        }

        _queue.DropRemaining();
    }

    private async Task HandleAsync(BackgroundEnvelope<TItem> envelope, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IBackgroundItemHandler<TItem>>();

            await handler.HandleAsync(envelope.Item, envelope.ParentContext, cancellationToken);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            _queue.ReportDropped(envelope.Item);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception, message: "A handler of the background queue of {ItemType} failed.", typeof(TItem).Name);
        }
    }
}
