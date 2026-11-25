using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.UnitTests.Chats.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Insights.UnitTests.Chats;

[Feature(TestFeatures.Chat)]
public sealed class ChatSnapshotThrottleTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(value: 150);

    private readonly FakeTimeProvider _time = new(ChatTestData.Now);
    private readonly RecordingRealtimeNotifier _notifier = new();
    private readonly ChatSnapshotThrottle _throttle;

    public ChatSnapshotThrottleTests()
    {
        _throttle = new ChatSnapshotThrottle(
            _notifier, _time, Interval, ChatTestData.OwnerId, ChatTestData.CompanyId, ChatTestData.MachineId);
    }

    [Fact]
    public void Append_WithinTheInterval_PushesOnlyTheFirstPieceThenTheFullTextAfterTheInterval()
    {
        _throttle.Append(piece: "a");
        _time.Advance(TimeSpan.FromMilliseconds(milliseconds: 50));
        _throttle.Append(piece: "b");
        _time.Advance(TimeSpan.FromMilliseconds(milliseconds: 100));
        _throttle.Append(piece: "c");

        _notifier.Updates.Select(update => update.Text).Should().Equal("a", "abc");
        _notifier.Updates.Select(update => update.Sequence).Should().Equal(1, 2);
    }

    [Fact]
    public void Flush_WithPendingText_PushesTheFullTextOnce()
    {
        _throttle.Append(piece: "a");
        _time.Advance(TimeSpan.FromMilliseconds(milliseconds: 10));
        _throttle.Append(piece: "b");

        _throttle.Flush();
        _throttle.Flush();

        _notifier.Updates.Select(update => update.Text).Should().Equal("a", "ab");
        _notifier.Updates.Select(update => update.Sequence).Should().Equal(1, 2);
    }

    [Fact]
    public void Flush_WithoutPendingText_PushesNothing()
    {
        _throttle.Flush();

        _notifier.Updates.Should().BeEmpty();
    }
}
