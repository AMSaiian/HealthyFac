using HealthyFac.Shared.Application.BackgroundQueues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.Infrastructure.BackgroundQueues;

/// <summary>
/// Fails the host start when the item type has no handler or more than 1 handler.
/// </summary>
/// <typeparam name="TItem">The type of the queued item.</typeparam>
/// <param name="scopeFactory">Creates the scope that resolves the handlers.</param>
internal sealed class BackgroundQueueStartCheck<TItem>(IServiceScopeFactory scopeFactory) : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var handlerCount = scope.ServiceProvider.GetServices<IBackgroundItemHandler<TItem>>().Count();

        return handlerCount == 1
            ? Task.CompletedTask
            : throw BackgroundQueueErrors.HandlerCount(typeof(TItem), handlerCount);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
