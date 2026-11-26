using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.Chats.Models;
using HealthyFac.Insights.UnitTests.Chats.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Insights.UnitTests.Chats;

[Feature(TestFeatures.Chat)]
public sealed class StallGuardChatClientTests
{
    private const int MaxWaitSeconds = 120;

    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(seconds: 30);

    private readonly FakeTimeProvider _time = new();
    private readonly StallScriptChatClient _inner;
    private readonly FakeLogger _logger = new();
    private readonly StallGuardChatClient _guard;

    public StallGuardChatClientTests()
    {
        _inner = new StallScriptChatClient(_time);
        _guard = new StallGuardChatClient(_inner, IdleTimeout, _time, _logger);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenTheFirstRequestStallsBeforeAnyUpdate_SendsTheRequestOnceMore()
    {
        _inner.Script(new StallStep(IsStall: true));
        _inner.Script(new StallStep(Text: "one"), new StallStep(Text: "two"));

        var texts = await ReadTextsAsync(TestContext.Current.CancellationToken);

        texts.Should().Equal("one", "two");
        _inner.RequestCount.Should().Be(expected: 2);

        _logger.Collector.GetSnapshot()
            .Where(entry => entry.Level == LogLevel.Warning)
            .Should()
            .ContainSingle();
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenBothRequestsStall_ThrowsAProviderUnavailableException()
    {
        _inner.Script(new StallStep(IsStall: true));
        _inner.Script(new StallStep(IsStall: true));

        var act = () => ReadTextsAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
        _inner.RequestCount.Should().Be(expected: 2);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenTheStreamStallsAfterAnUpdate_ThrowsWithoutANewRequest()
    {
        _inner.Script(new StallStep(Text: "one"), new StallStep(IsStall: true));

        var act = () => ReadTextsAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
        _inner.RequestCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenTheCallerCancelsDuringTheWait_ThrowsOperationCanceled()
    {
        _inner.Script(new StallStep(IsStall: true));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var read = ReadTextsAsync(cancellation.Token);

        while (_inner.RequestCount == 0)
        {
            await Task.Delay(millisecondsDelay: 5, CancellationToken.None);
        }

        _time.Advance(TimeSpan.FromSeconds(seconds: 5));
        await cancellation.CancelAsync();
        var act = () => read;

        await act.Should().ThrowAsync<OperationCanceledException>();
        _inner.RequestCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenUpdatesComeWithinTheIdleTimeout_DeliversAllWithoutARetry()
    {
        var gap = TimeSpan.FromSeconds(seconds: 20);

        _inner.Script(
            new StallStep(Text: "one", gap),
            new StallStep(Text: "two", gap),
            new StallStep(Text: "three", gap));

        var texts = await ReadTextsAsync(TestContext.Current.CancellationToken);

        texts.Should().Equal("one", "two", "three");
        _inner.RequestCount.Should().Be(expected: 1);
        _logger.Collector.GetSnapshot().Where(entry => entry.Level == LogLevel.Warning).Should().BeEmpty();
    }

    private async Task<List<string>> ReadTextsAsync(CancellationToken cancellationToken)
    {
        var texts = new List<string>();

        var read = Task.Run(
            async () =>
            {
                await foreach (var update in _guard.GetStreamingResponseAsync([], options: null, cancellationToken))
                {
                    texts.Add(update.Text);
                }
            },
            cancellationToken);

        for (var second = 0; second < MaxWaitSeconds && !read.IsCompleted; second++)
        {
            _time.Advance(TimeSpan.FromSeconds(seconds: 1));
            await Task.WhenAny(read, Task.Delay(millisecondsDelay: 5, CancellationToken.None));
        }

        read.IsCompleted.Should().BeTrue(because: "the guard ends a stalled stream");
        await read;

        return texts;
    }
}
