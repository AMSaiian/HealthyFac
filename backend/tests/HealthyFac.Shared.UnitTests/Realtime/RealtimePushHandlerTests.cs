using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.UnitTests.Realtime.Data;
using HealthyFac.Testing.Tracing;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace HealthyFac.Shared.UnitTests.Realtime;

public sealed class RealtimePushHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRedisFailsOnEveryAttempt_SendsFourTimesLogsOneWarningAndCountsFailed()
    {
        using var fixture = new PushHandlerFixture();
        fixture.Client.Failure = RealtimePushHandlerTestData.Failure;
        using var pushes = fixture.CollectPushes();
        using var durations = fixture.CollectDurations();

        await fixture.HandleAsync(RealtimePushHandlerTestData.UserPush());

        var measurement = pushes.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        var duration = durations.GetMeasurementSnapshot().Should().ContainSingle().Subject;

        fixture.Client.SendCount.Should().Be(expected: 4);
        fixture.Logger.Collector.GetSnapshot().Should().ContainSingle(entry => entry.Level == LogLevel.Warning);

        measurement.Tags[key: "realtime.result"].Should().Be(expected: "failed");

        duration.Tags[key: "realtime.result"].Should().Be(expected: "failed");
        duration.Value.Should().BeGreaterThanOrEqualTo(RealtimePushHandlerTestData.MinimumRetrySeconds);
    }

    [Fact]
    public async Task HandleAsync_WhenTheBreakerIsOpen_SendsNothingAndCountsDropped()
    {
        var control = new CircuitBreakerManualControl();
        await control.IsolateAsync(TestContext.Current.CancellationToken);
        var attempts = new AttemptCounter();
        var openBreaker = new ResiliencePipelineBuilder()
            .AddStrategy(_ => new CountingStrategy(attempts), new CountingStrategyOptions())
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions { ManualControl = control })
            .Build();
        using var fixture = new PushHandlerFixture(openBreaker);
        using var pushes = fixture.CollectPushes();
        using var durations = fixture.CollectDurations();

        await fixture.HandleAsync(RealtimePushHandlerTestData.UserPush());

        var measurement = pushes.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        var duration = durations.GetMeasurementSnapshot().Should().ContainSingle().Subject;

        fixture.Client.SendCount.Should().Be(expected: 0);
        attempts.Count.Should().Be(expected: 1);
        fixture.Logger.Collector.GetSnapshot().Should().BeEmpty();

        measurement.Tags[key: "realtime.result"].Should().Be(expected: "dropped");

        duration.Tags[key: "realtime.result"].Should().Be(expected: "dropped");
    }

    [Fact]
    public async Task HandleAsync_WhenTheSendSucceeds_CountsSentWithTheKindAndNoUserId()
    {
        using var fixture = new PushHandlerFixture();
        using var pushes = fixture.CollectPushes();
        using var durations = fixture.CollectDurations();

        await fixture.HandleAsync(RealtimePushHandlerTestData.UserPush());

        var measurement = pushes.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        var duration = durations.GetMeasurementSnapshot().Should().ContainSingle().Subject;

        fixture.Client.SendCount.Should().Be(expected: 1);
        fixture.Clients.UserCalls.Should().Equal(RealtimePushHandlerTestData.UserId.ToString());

        measurement.Tags[key: "realtime.target"].Should().Be(expected: "shiftChanged");
        measurement.Tags[key: "realtime.result"].Should().Be(expected: "sent");
        measurement.Tags.Values.Select(value => value?.ToString())
            .Should()
            .NotContain(value => value!.Contains(RealtimePushHandlerTestData.UserId.ToString()));

        duration.Tags[key: "realtime.target"].Should().Be(expected: "shiftChanged");
        duration.Tags[key: "realtime.result"].Should().Be(expected: "sent");
        duration.Tags.Values.Select(value => value?.ToString())
            .Should()
            .NotContain(value => value!.Contains(RealtimePushHandlerTestData.UserId.ToString()));
    }

    [Fact]
    public async Task HandleAsync_WithParentContext_StartsThePushSpanUnderIt()
    {
        using var fixture = new PushHandlerFixture();
        using var capture = new SpanCapture(ObservabilityConstants.Sources.Realtime);

        await fixture.HandleAsync(RealtimePushHandlerTestData.UserPush(), RealtimePushHandlerTestData.ParentContext);

        var span = capture.Spans
            .Should()
            .ContainSingle(candidate => candidate.TraceId == RealtimePushHandlerTestData.ParentContext.TraceId)
            .Subject;
        span.OperationName.Should().Be(expected: "Realtime.Push");
        span.ParentSpanId.Should().Be(RealtimePushHandlerTestData.ParentContext.SpanId);
        span.GetTagItem(key: "realtime.target").Should().Be(expected: "shiftChanged");
        span.GetTagItem(key: "realtime.result").Should().Be(expected: "sent");
    }

    [Fact]
    public void QueueLengthGauge_WithQueuedItems_ReportsTheCount()
    {
        using var fixture = new PushHandlerFixture();
        using var length = fixture.CollectQueueLength();
        fixture.Queue.Enqueue(RealtimePushHandlerTestData.UserPush(), default(ActivityContext));
        fixture.Queue.Enqueue(RealtimePushHandlerTestData.UserPush(), default(ActivityContext));

        length.RecordObservableInstruments();

        var measurement = length.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        measurement.Value.Should().Be(expected: 2);
        measurement.Tags[key: "background_queue.item_type"].Should().Be(expected: "realtimePush");
    }
}
