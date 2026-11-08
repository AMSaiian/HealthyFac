using System.Diagnostics;
using HealthyFac.Telemetry.Application.Intake;
using HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;
using HealthyFac.Telemetry.IntegrationTests.Intake.Data;
using HealthyFac.Testing.Tracing;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Options;
using Constants = HealthyFac.Shared.Application.Observability.ObservabilityConstants;

namespace HealthyFac.Telemetry.IntegrationTests.Intake;

public sealed class IntakeObservabilityTests(MqttIntakeFixture fixture) : IClassFixture<MqttIntakeFixture>
{
    private const string TraceParentHeader = "traceparent";

    private readonly MqttIntakeFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(IntakeObservabilityTestData.Results), MemberType = typeof(IntakeObservabilityTestData))]
    public async Task Intake_WithMessage_RecordsConsumerSpanCounterAndDuration(IntakeResultCase scenario)
    {
        await _fixture.WaitUntilConnectedAsync();
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = scenario.IsLongTopic
            ? ReceiveMachineMessageTestData.TopicFor(IntakeObservabilityTestData.LongGatewayId)
            : ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        var payload = scenario.Json is null
            ? ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage())
            : ReceiveMachineMessageTestData.Bytes(scenario.Json);
        using var capture = new SpanCapture(Constants.Intake.Source);
        using var counter = Collect<long>(Constants.Intake.Instruments.Messages);
        using var duration = Collect<double>(Constants.Intake.Instruments.HandleDuration);
        await PublishRepeatedFirstAsync(scenario, topic, payload, capture);
        if (scenario.IsFailureArmed)
        {
            _fixture.SaveFailure.Arm(new InvalidOperationException(message: "The test made this save fail."));
        }

        await _fixture.PublishAsync(topic, payload);

        var expectedSpans = scenario.IsRepeated ? 2 : 1;
        var spans = await capture.WaitForSpansAsync(span => IsOnTopic(span, topic), expectedSpans);
        var span = spans.Last();
        var counted = counter.GetMeasurementSnapshot()
            .Where(measurement => HasResult(measurement, scenario.Result))
            .ToList();
        var timed = duration.GetMeasurementSnapshot()
            .Where(measurement => HasResult(measurement, scenario.Result))
            .ToList();
        var expectedStatus = scenario.Result == Constants.Intake.Results.Failed
            ? ActivityStatusCode.Error
            : ActivityStatusCode.Unset;

        spans.Should().HaveCount(expectedSpans);
        span.DisplayName.Should().Be(Constants.Intake.SpanName);
        span.Kind.Should().Be(ActivityKind.Consumer);
        span.GetTagItem(Constants.Messaging.Tags.SystemName).Should().Be(Constants.Intake.MqttSystem);
        span.GetTagItem(Constants.Messaging.Tags.OperationType).Should().Be(Constants.Messaging.Operations.Process);
        span.GetTagItem(Constants.Messaging.Tags.DestinationName).Should().Be(topic);
        span.GetTagItem(Constants.Intake.Tags.ConsumerGroup).Should().Be(SharedGroup());
        span.GetTagItem(Constants.Intake.Tags.ConsumerIndex).Should().Be(expected: 0);
        span.GetTagItem(Constants.Intake.Tags.Result).Should().Be(scenario.Result);
        span.GetTagItem(Constants.Intake.Tags.ErrorCode).Should().Be(scenario.ErrorCode);
        span.Status.Should().Be(expectedStatus);
        counted.Should().ContainSingle().Which.Value.Should().Be(expected: 1);
        counted.Single().Tags.GetValueOrDefault(Constants.Intake.Tags.ErrorCode).Should().Be(scenario.ErrorCode);
        timed.Should().ContainSingle().Which.Value.Should().BeGreaterThanOrEqualTo(expected: 0);
    }

    [Fact]
    public async Task Intake_WithTransientFailureOnce_RecordsRedeliveryThenStored()
    {
        await _fixture.WaitUntilConnectedAsync();
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        using var capture = new SpanCapture(Constants.Intake.Source);
        using var counter = Collect<long>(Constants.Intake.Instruments.Messages);
        _fixture.SaveFailure.Arm(new TimeoutException());

        await _fixture.PublishAsync(
            topic,
            ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage()));

        var spans = await capture.WaitForSpansAsync(span => IsOnTopic(span, topic), expectedCount: 2);
        var redelivery = spans.Single(span => HasResultTag(span, Constants.Intake.Results.Redelivery));
        var results = counter.GetMeasurementSnapshot()
            .Select(measurement => measurement.Tags.GetValueOrDefault(Constants.Intake.Tags.Result))
            .ToList();

        redelivery.Status.Should().Be(ActivityStatusCode.Error);
        spans.Should().ContainSingle(span => HasResultTag(span, Constants.Intake.Results.Stored));
        results.Should().BeEquivalentTo([Constants.Intake.Results.Redelivery, Constants.Intake.Results.Stored]);
    }

    [Fact]
    public async Task Intake_WithMessage_StartsTheUseCaseSpanUnderTheConsumerSpan()
    {
        await _fixture.WaitUntilConnectedAsync();
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        using var capture = new SpanCapture(Constants.Intake.Source, Constants.Sources.UseCases);

        await _fixture.PublishAsync(
            topic,
            ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage()));

        var consumer = (await capture.WaitForSpansAsync(span => IsOnTopic(span, topic), expectedCount: 1)).Single();
        var useCase = capture.Spans.Single(
            span => span.Source.Name == Constants.Sources.UseCases
                && span.DisplayName == nameof(ReceiveMachineMessageRequest)
                && span.TraceId == consumer.TraceId);

        useCase.ParentSpanId.Should().Be(consumer.SpanId);
    }

    [Fact]
    public async Task Intake_WithTraceParentProperty_ContinuesThatTrace()
    {
        await _fixture.WaitUntilConnectedAsync();
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        var traceId = ActivityTraceId.CreateRandom();
        var parentId = ActivitySpanId.CreateRandom();
        using var capture = new SpanCapture(Constants.Intake.Source);

        await _fixture.PublishWithPropertyAsync(
            topic,
            ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage()),
            TraceParentHeader,
            $"00-{traceId}-{parentId}-01");

        var span = (await capture.WaitForSpansAsync(span => IsOnTopic(span, topic), expectedCount: 1)).Single();

        span.TraceId.Should().Be(traceId);
        span.ParentSpanId.Should().Be(parentId);
    }

    [Fact]
    public async Task Intake_WithoutTraceParentProperty_StartsANewTrace()
    {
        await _fixture.WaitUntilConnectedAsync();
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        using var capture = new SpanCapture(Constants.Intake.Source);

        await _fixture.PublishAsync(
            topic,
            ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage()));

        var span = (await capture.WaitForSpansAsync(span => IsOnTopic(span, topic), expectedCount: 1)).Single();

        span.Parent.Should().BeNull();
        span.ParentSpanId.Should().Be(default(ActivitySpanId));
    }

    [Fact]
    public async Task Intake_WithStoredMessage_RecordsTheLagBetweenSamplingAndReceipt()
    {
        await _fixture.WaitUntilConnectedAsync();
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        var sampledAt = DateTime.UtcNow - IntakeObservabilityTestData.Lag;
        using var capture = new SpanCapture(Constants.Intake.Source);
        using var lag = Collect<double>(Constants.Intake.Instruments.Lag);

        await _fixture.PublishAsync(
            topic,
            ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage(sampledAt)));

        await capture.WaitForSpansAsync(span => IsOnTopic(span, topic), expectedCount: 1);
        var measurement = lag.GetMeasurementSnapshot().Single();

        measurement.Value.Should()
            .BeInRange(
                IntakeObservabilityTestData.Lag.TotalSeconds,
                IntakeObservabilityTestData.Lag.TotalSeconds + 30);
        measurement.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task Intake_WithRejectedMessage_RecordsNoLag()
    {
        await _fixture.WaitUntilConnectedAsync();
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        using var capture = new SpanCapture(Constants.Intake.Source);
        using var lag = Collect<double>(Constants.Intake.Instruments.Lag);

        await _fixture.PublishAsync(
            topic,
            ReceiveMachineMessageTestData.Bytes(IntakeObservabilityTestData.EnvelopeFailureJson));

        await capture.WaitForSpansAsync(span => IsOnTopic(span, topic), expectedCount: 1);

        lag.GetMeasurementSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task Intake_WhileConnected_ReportsTheConsumerCountAsConnectedSessions()
    {
        await _fixture.WaitUntilConnectedAsync();
        using var connected = Collect<int>(Constants.Intake.Instruments.ConsumersConnected);

        connected.RecordObservableInstruments();

        var expectedCount = _fixture.Database.GetSingleton<IOptions<MqttIntakeOptions>>().Value.ConsumerCount;
        connected.LastMeasurement!.Value.Should().Be(expectedCount);
    }

    [Fact]
    public async Task Intake_WhileASaveRuns_ReportsOneMessageInFlight()
    {
        await _fixture.WaitUntilConnectedAsync();
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        using var capture = new SpanCapture(Constants.Intake.Source);
        using var inFlight = Collect<int>(Constants.Intake.Instruments.InFlight);
        _fixture.SlowSave.Arm(IntakeObservabilityTestData.SlowSaveDelay);

        await _fixture.PublishAsync(
            topic,
            ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage()));

        var whileRunning = await ObserveInFlightUntilAsync(inFlight, expected: 1);
        await capture.WaitForSpansAsync(span => IsOnTopic(span, topic), expectedCount: 1);
        inFlight.RecordObservableInstruments();

        whileRunning.Should().Be(expected: 1);
        inFlight.LastMeasurement!.Value.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Intake_WithMessage_UsesOnlyAllowedTagsAndNoGatewayId()
    {
        await _fixture.WaitUntilConnectedAsync();
        var machine = await _fixture.Database.ArrangeIntakeMachineAsync();
        var topic = ReceiveMachineMessageTestData.TopicFor(machine.GatewayId);
        using var capture = new SpanCapture(Constants.Intake.Source);
        using var counter = Collect<long>(Constants.Intake.Instruments.Messages);
        using var duration = Collect<double>(Constants.Intake.Instruments.HandleDuration);

        await _fixture.PublishAsync(
            topic,
            ReceiveMachineMessageTestData.Bytes(ReceiveMachineMessageTestData.CreateMessage()));

        var span = (await capture.WaitForSpansAsync(span => IsOnTopic(span, topic), expectedCount: 1)).Single();
        var tags = counter.GetMeasurementSnapshot()
            .SelectMany(measurement => measurement.Tags)
            .Concat(duration.GetMeasurementSnapshot().SelectMany(measurement => measurement.Tags))
            .ToList();

        tags.Should().NotBeEmpty();
        tags.Select(tag => tag.Key)
            .Should()
            .OnlyContain(key => IntakeObservabilityTestData.AllowedTagKeys.Contains(key));
        tags.Select(tag => tag.Value?.ToString()).Should().NotContain(machine.GatewayId);
        span.DisplayName.Should().NotContain(machine.GatewayId);
    }

    private async Task PublishRepeatedFirstAsync(
        IntakeResultCase scenario,
        string topic,
        byte[] payload,
        SpanCapture capture
    )
    {
        if (!scenario.IsRepeated)
        {
            return;
        }

        await _fixture.PublishAsync(topic, payload);
        await capture.WaitForSpansAsync(span => IsOnTopic(span, topic), expectedCount: 1);
    }

    private static async Task<int> ObserveInFlightUntilAsync(MetricCollector<int> inFlight, int expected)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(seconds: 10);
        var observed = 0;

        while (observed != expected && DateTime.UtcNow < deadline)
        {
            inFlight.RecordObservableInstruments();
            observed = inFlight.LastMeasurement?.Value ?? 0;
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 50));
        }

        return observed;
    }

    private MetricCollector<T> Collect<T>(string instrument)
        where T : struct =>
        new(_fixture.MeterFactory, Constants.Intake.Meter, instrument);

    private string SharedGroup() => _fixture.Database.GetSingleton<IOptions<MqttIntakeOptions>>().Value.SharedGroup;

    private static bool IsOnTopic(Activity span, string topic) =>
        Equals(span.GetTagItem(Constants.Messaging.Tags.DestinationName), topic);

    private static bool HasResultTag(Activity span, string result) =>
        Equals(span.GetTagItem(Constants.Intake.Tags.Result), result);

    private static bool HasResult<T>(CollectedMeasurement<T> measurement, string result)
        where T : struct =>
        Equals(measurement.Tags.GetValueOrDefault(Constants.Intake.Tags.Result), result);
}
