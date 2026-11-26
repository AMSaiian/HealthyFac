using System.Diagnostics;
using HealthyFac.Shared.Application.BackgroundQueues;
using HealthyFac.Shared.Infrastructure.BackgroundQueues;
using HealthyFac.Shared.UnitTests.BackgroundQueues.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace HealthyFac.Shared.UnitTests.BackgroundQueues;

[Feature(TestFeatures.Platform)]
public sealed class BackgroundQueueTests
{
    [Fact]
    public async Task Enqueue_WithItem_DeliversTheItemAndItsParentContextToTheHandler()
    {
        var log = new HandlerLog();
        log.Gate.SetResult();
        await using var provider = BackgroundQueueTestHost.Build(log, handlerCount: 1);
        await BackgroundQueueTestHost.StartAsync(provider, TestContext.Current.CancellationToken);

        provider.GetRequiredService<IBackgroundQueue<QueueTestItem>>()
            .Enqueue(new QueueTestItem { Number = 1 }, BackgroundQueueTestHost.ParentContext);

        await BackgroundQueueTestHost.WaitUntilAsync(() => !log.Entries.IsEmpty, TestContext.Current.CancellationToken);
        await BackgroundQueueTestHost.StopAsync(provider, TestContext.Current.CancellationToken);

        var entry = log.Entries.Should().ContainSingle().Subject;
        entry.Item.Number.Should().Be(expected: 1);
        entry.ParentContext.Should().Be(BackgroundQueueTestHost.ParentContext);
    }

    [Fact]
    public async Task Enqueue_WithTwoItems_RunsEachItemInItsOwnScope()
    {
        var log = new HandlerLog();
        log.Gate.SetResult();
        await using var provider = BackgroundQueueTestHost.Build(log, handlerCount: 1);
        await BackgroundQueueTestHost.StartAsync(provider, TestContext.Current.CancellationToken);
        var queue = provider.GetRequiredService<IBackgroundQueue<QueueTestItem>>();

        queue.Enqueue(new QueueTestItem { Number = 1 }, default(ActivityContext));
        queue.Enqueue(new QueueTestItem { Number = 2 }, default(ActivityContext));

        await BackgroundQueueTestHost.WaitUntilAsync(
            () => log.Entries.Count == 2,
            TestContext.Current.CancellationToken);

        await BackgroundQueueTestHost.StopAsync(provider, TestContext.Current.CancellationToken);

        log.Entries.Select(entry => entry.ScopeId).Distinct().Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task Enqueue_WhileTheHandlerBlocks_ReturnsAtOnceAndKeepsTheItem()
    {
        var log = new HandlerLog();
        await using var provider = BackgroundQueueTestHost.Build(log, handlerCount: 1);
        await BackgroundQueueTestHost.StartAsync(provider, TestContext.Current.CancellationToken);
        var queue = provider.GetRequiredService<IBackgroundQueue<QueueTestItem>>();
        queue.Enqueue(new QueueTestItem { Number = 1 }, default(ActivityContext));
        await log.Started.Task.WaitAsync(BackgroundQueueTestHost.Wait, TestContext.Current.CancellationToken);

        await Task.Run(
                () => queue.Enqueue(new QueueTestItem { Number = 2 }, default(ActivityContext)),
                TestContext.Current.CancellationToken)
            .WaitAsync(BackgroundQueueTestHost.EnqueueLimit, TestContext.Current.CancellationToken);

        var count = provider.GetRequiredService<BackgroundQueue<QueueTestItem>>().Count;
        log.Gate.SetResult();
        await BackgroundQueueTestHost.StopAsync(provider, TestContext.Current.CancellationToken);

        count.Should().Be(expected: 1);
        log.Entries.Select(entry => entry.Item.Number).Should().Equal(1, 2);
    }

    [Fact]
    public async Task StopAsync_WhenItsTokenFires_ReportsTheItemInWorkAndTheRestAsDropped()
    {
        var log = new HandlerLog();
        await using var provider = BackgroundQueueTestHost.Build(log, handlerCount: 1, hasDropObserver: true);
        await BackgroundQueueTestHost.StartAsync(provider, TestContext.Current.CancellationToken);
        var queue = provider.GetRequiredService<IBackgroundQueue<QueueTestItem>>();
        queue.Enqueue(new QueueTestItem { Number = 1 }, default(ActivityContext));
        await log.Started.Task.WaitAsync(BackgroundQueueTestHost.Wait, TestContext.Current.CancellationToken);
        queue.Enqueue(new QueueTestItem { Number = 2 }, default(ActivityContext));
        queue.Enqueue(new QueueTestItem { Number = 3 }, default(ActivityContext));
        using var stopLimit = new CancellationTokenSource(TimeSpan.FromMilliseconds(milliseconds: 300));

        await BackgroundQueueTestHost.StopAsync(provider, stopLimit.Token);

        log.Entries.Should().BeEmpty();
        log.Dropped.Order().Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task StopAsync_WhenTheHandlerFailsOnCancel_ReportsItDroppedWithoutAWarning()
    {
        var log = new HandlerLog { IsFailingOnCancel = true };
        var logs = new FakeLoggerProvider();

        await using var provider = BackgroundQueueTestHost.Build(
            log,
            handlerCount: 1,
            hasDropObserver: true,
            logs);

        await BackgroundQueueTestHost.StartAsync(provider, TestContext.Current.CancellationToken);

        provider.GetRequiredService<IBackgroundQueue<QueueTestItem>>()
            .Enqueue(new QueueTestItem { Number = 1 }, default(ActivityContext));

        await log.Started.Task.WaitAsync(BackgroundQueueTestHost.Wait, TestContext.Current.CancellationToken);
        using var stopLimit = new CancellationTokenSource(TimeSpan.FromMilliseconds(milliseconds: 300));

        await BackgroundQueueTestHost.StopAsync(provider, stopLimit.Token);

        log.Dropped.Should().Equal(1);
        logs.Collector.GetSnapshot().Should().NotContain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task StopAsync_WhenTheHandlerCleansUpAfterCancel_ReturnsAfterTheDropsAreCounted()
    {
        var log = new HandlerLog { CancelCleanup = TimeSpan.FromMilliseconds(milliseconds: 300) };
        await using var provider = BackgroundQueueTestHost.Build(log, handlerCount: 1, hasDropObserver: true);
        await BackgroundQueueTestHost.StartAsync(provider, TestContext.Current.CancellationToken);
        var queue = provider.GetRequiredService<IBackgroundQueue<QueueTestItem>>();
        queue.Enqueue(new QueueTestItem { Number = 1 }, default(ActivityContext));
        await log.Started.Task.WaitAsync(BackgroundQueueTestHost.Wait, TestContext.Current.CancellationToken);
        queue.Enqueue(new QueueTestItem { Number = 2 }, default(ActivityContext));
        using var stopLimit = new CancellationTokenSource(TimeSpan.FromMilliseconds(milliseconds: 100));

        await BackgroundQueueTestHost.StopAsync(provider, stopLimit.Token);

        log.Dropped.Order().Should().Equal(1, 2);
    }

    [Fact]
    public async Task StopAsync_BeforeItsTokenFires_HandlesTheQueuedItems()
    {
        var log = new HandlerLog();
        await using var provider = BackgroundQueueTestHost.Build(log, handlerCount: 1, hasDropObserver: true);
        await BackgroundQueueTestHost.StartAsync(provider, TestContext.Current.CancellationToken);
        var queue = provider.GetRequiredService<IBackgroundQueue<QueueTestItem>>();
        queue.Enqueue(new QueueTestItem { Number = 1 }, default(ActivityContext));
        queue.Enqueue(new QueueTestItem { Number = 2 }, default(ActivityContext));
        await log.Started.Task.WaitAsync(BackgroundQueueTestHost.Wait, TestContext.Current.CancellationToken);
        log.Gate.SetResult();

        await BackgroundQueueTestHost.StopAsync(provider, TestContext.Current.CancellationToken);

        log.Entries.Select(entry => entry.Item.Number).Should().Equal(1, 2);
        log.Dropped.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(BackgroundQueueTestData.WrongHandlerCounts), MemberType = typeof(BackgroundQueueTestData))]
    public async Task StartAsync_WithoutExactlyOneHandler_Throws(string caseName, int handlerCount)
    {
        await using var provider = BackgroundQueueTestHost.Build(new HandlerLog(), handlerCount);

        var act = () => BackgroundQueueTestHost.StartAsync(provider, TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"*needs exactly 1 handler, but {handlerCount} are registered*", caseName);
    }

    [Fact]
    public async Task Enqueue_AfterAFailingHandler_HandlesTheNextItem()
    {
        var log = new HandlerLog { IsFailing = number => number == 1 };
        log.Gate.SetResult();
        await using var provider = BackgroundQueueTestHost.Build(log, handlerCount: 1);
        await BackgroundQueueTestHost.StartAsync(provider, TestContext.Current.CancellationToken);
        var queue = provider.GetRequiredService<IBackgroundQueue<QueueTestItem>>();

        queue.Enqueue(new QueueTestItem { Number = 1 }, default(ActivityContext));
        queue.Enqueue(new QueueTestItem { Number = 2 }, default(ActivityContext));
        await BackgroundQueueTestHost.WaitUntilAsync(() => !log.Entries.IsEmpty, TestContext.Current.CancellationToken);
        await BackgroundQueueTestHost.StopAsync(provider, TestContext.Current.CancellationToken);

        log.Entries.Select(entry => entry.Item.Number).Should().Equal(2);
    }
}
