using System.Collections.Concurrent;
using System.Diagnostics;
using HealthyFac.Shared.Application.BackgroundQueues;
using HealthyFac.Shared.Infrastructure.BackgroundQueues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace HealthyFac.Shared.UnitTests.BackgroundQueues.Data;

public interface IQueueTestItem
{
    int Number { get; }
}

public sealed class QueueTestItem : IQueueTestItem
{
    public required int Number { get; init; }
}

public sealed class ScopedProbe
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class HandledEntry
{
    public required QueueTestItem Item { get; init; }

    public required ActivityContext ParentContext { get; init; }

    public required Guid ScopeId { get; init; }
}

public sealed class HandlerLog
{
    public ConcurrentQueue<HandledEntry> Entries { get; } = new();

    public ConcurrentQueue<int> Dropped { get; } = new();

    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Func<int, bool> IsFailing { get; set; } = _ => false;

    public bool IsFailingOnCancel { get; set; }

    public TimeSpan CancelCleanup { get; set; }
}

/// <summary>
/// Generic on purpose: the convention scan of the item types skips an open generic handler, so the tests can
/// register this handler 0 or 2 times without a planted violation.
/// </summary>
public sealed class RecordingHandler<TItem>(HandlerLog log, ScopedProbe probe) : IBackgroundItemHandler<TItem>
    where TItem : IQueueTestItem
{
    private readonly HandlerLog _log = log;
    private readonly ScopedProbe _probe = probe;

    public async Task HandleAsync(TItem item, ActivityContext parentContext, CancellationToken cancellationToken)
    {
        _log.Started.TrySetResult();
        if (_log.IsFailing(item.Number))
        {
            throw new InvalidOperationException(message: "The test handler fails.");
        }

        try
        {
            await _log.Gate.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (_log.IsFailingOnCancel)
        {
            throw new InvalidOperationException(message: "The test handler fails on cancel.");
        }
        catch (OperationCanceledException)
        {
            await Task.Delay(_log.CancelCleanup, CancellationToken.None);

            throw;
        }

        _log.Entries.Enqueue(
            new HandledEntry
            {
                Item = new QueueTestItem { Number = item.Number }, ParentContext = parentContext, ScopeId = _probe.Id,
            });
    }
}

public sealed class RecordingDropObserver(HandlerLog log) : IBackgroundItemDropObserver<QueueTestItem>
{
    private readonly HandlerLog _log = log;

    public void OnDropped(QueueTestItem item) => _log.Dropped.Enqueue(item.Number);
}

public static class BackgroundQueueTestData
{
    public static TheoryData<string, int> WrongHandlerCounts => new() { { "none", 0 }, { "two", 2 } };
}

public static class BackgroundQueueTestHost
{
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(seconds: 10);

    public static readonly TimeSpan EnqueueLimit = TimeSpan.FromSeconds(seconds: 2);

    public static readonly ActivityContext ParentContext = new(
        ActivityTraceId.CreateFromString(idData: "0af7651916cd43dd8448eb211c80319c"),
        ActivitySpanId.CreateFromString(idData: "b7ad6b7169203331"),
        ActivityTraceFlags.Recorded);

    public static ServiceProvider Build(
        HandlerLog log,
        int handlerCount,
        bool hasDropObserver = false,
        FakeLoggerProvider? loggerProvider = null
    )
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            if (loggerProvider is not null)
            {
                builder.AddProvider(loggerProvider);
            }
        });
        services.AddSingleton(log);
        services.AddScoped<ScopedProbe>();
        if (hasDropObserver)
        {
            services.AddSingleton<IBackgroundItemDropObserver<QueueTestItem>, RecordingDropObserver>();
        }

        for (var index = 0; index < handlerCount; index++)
        {
            services.AddScoped<IBackgroundItemHandler<QueueTestItem>, RecordingHandler<QueueTestItem>>();
        }

        services.AddBackgroundQueue<QueueTestItem>(BackgroundQueueCapacity.Unbounded);

        return services.BuildServiceProvider();
    }

    public static async Task StartAsync(ServiceProvider provider, CancellationToken cancellationToken)
    {
        foreach (var service in provider.GetServices<IHostedService>())
        {
            await service.StartAsync(cancellationToken);
        }
    }

    public static async Task StopAsync(ServiceProvider provider, CancellationToken cancellationToken)
    {
        foreach (var service in provider.GetServices<IHostedService>())
        {
            await service.StopAsync(cancellationToken);
        }
    }

    public static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + Wait;
        while (!condition() && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 20), cancellationToken);
        }
    }
}
