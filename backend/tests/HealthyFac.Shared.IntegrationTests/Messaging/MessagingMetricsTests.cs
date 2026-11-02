using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Constants = HealthyFac.Shared.Application.Observability.ObservabilityConstants;
using Instruments = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Messaging.Instruments;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Messaging.Tags;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class MessagingMetricsTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    private readonly TransportFixture _fixture = fixture;

    [Fact]
    public async Task OutboxProcessor_WithPendingRow_CountsPublished()
    {
        await _fixture.AddPendingOutboxRowsAsync(
            MessagingMetricsTestData.PublishedMessageType,
            MessagingMetricsTestData.SingleRowCount);
        using var published = Collect<long>(Instruments.Published);
        var processor = _fixture.Create<OutboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var measurements = published.ForMessageType(MessagingMetricsTestData.PublishedMessageType);

        var measurement = measurements.Should().ContainSingle().Which;
        measurement.Value.Should().Be(MessagingMetricsTestData.OneMessage);
        measurement.Tags.Should().Contain(Tags.Processor, Constants.Messaging.Processors.Outbox);
    }

    [Fact]
    public async Task InboxProcessor_WithPendingRow_CountsHandledAndRecordsRun()
    {
        await _fixture.AddPendingInboxRowAsync(
            MessagingMetricsTestData.HandledRowId,
            InboxProcessorTestData.RecordedMessageType,
            MessagingMetricsTestData.HandledText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);
        using var handled = Collect<long>(Instruments.Handled);
        using var runs = Collect<double>(Instruments.RunDuration);
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var handledMeasurements = handled.ForMessageType(InboxProcessorTestData.RecordedMessageType);
        var runMeasurements = runs.GetMeasurementSnapshot();

        var measurement = handledMeasurements.Should().ContainSingle().Which;
        measurement.Value.Should().Be(MessagingMetricsTestData.OneMessage);
        measurement.Tags.Should().Contain(Tags.Processor, Constants.Messaging.Processors.Inbox);

        var run = runMeasurements.Should().ContainSingle().Which;
        run.Tags.Should().Contain(Tags.Processor, Constants.Messaging.Processors.Inbox);
        run.Tags.Should().Contain(Constants.Tags.Outcome, Constants.Outcomes.Success);
    }

    [Fact]
    public async Task InboxProcessor_WhenHandlerThrows_CountsRetried()
    {
        await _fixture.AddPendingInboxRowAsync(
            MessagingMetricsTestData.RetriedRowId,
            InboxProcessorTestData.FailingMessageType,
            MessagingMetricsTestData.RetriedText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);
        using var retried = Collect<long>(Instruments.Retried);
        using var failed = Collect<long>(Instruments.Failed);
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var retriedMeasurements = retried.ForMessageType(InboxProcessorTestData.FailingMessageType);
        var failedMeasurements = failed.ForMessageType(InboxProcessorTestData.FailingMessageType);

        var measurement = retriedMeasurements.Should().ContainSingle().Which;
        measurement.Value.Should().Be(MessagingMetricsTestData.OneMessage);
        measurement.Tags.Should().Contain(Tags.Processor, Constants.Messaging.Processors.Inbox);
        failedMeasurements.Should().BeEmpty();
    }

    [Fact]
    public async Task InboxProcessor_WithInvalidPayload_CountsPermanentFailure()
    {
        await _fixture.AddPingInboxRowAsync(
            MessagingMetricsTestData.PermanentRowId,
            InboxProcessorTestData.InvalidPayload);
        using var failed = Collect<long>(Instruments.Failed);
        using var retried = Collect<long>(Instruments.Retried);
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var failedMeasurements = failed.ForMessageType(TransportTestData.PingMessageType);
        var retriedMeasurements = retried.ForMessageType(TransportTestData.PingMessageType);

        var measurement = failedMeasurements.Should().ContainSingle().Which;
        measurement.Value.Should().Be(MessagingMetricsTestData.OneMessage);
        measurement.Tags.Should().Contain(Tags.Failure, Constants.Messaging.Failures.Permanent);
        retriedMeasurements.Should().BeEmpty();
    }

    [Fact]
    public async Task InboxProcessor_AtAttemptLimit_CountsAttemptLimitFailure()
    {
        await _fixture.AddInboxRowAsync(
            InboxProcessorTestData.CreateRowWithAttempts(
                MessagingMetricsTestData.AttemptLimitRowId,
                InboxProcessorTestData.FailingMessageType,
                MessagingMetricsTestData.AttemptLimitText,
                InboxTestData.Now,
                InboxProcessorTestData.MaxAttemptCount - 1));
        using var failed = Collect<long>(Instruments.Failed);
        var processor = _fixture.Create<InboxProcessor>();

        await processor.RunAsync(CancellationToken.None);

        var measurements = failed.ForMessageType(InboxProcessorTestData.FailingMessageType);

        var measurement = measurements.Should().ContainSingle().Which;
        measurement.Value.Should().Be(MessagingMetricsTestData.OneMessage);
        measurement.Tags.Should().Contain(Tags.Failure, Constants.Messaging.Failures.AttemptLimit);
    }

    [Fact]
    public async Task InboxProcessor_WhenRunIsCancelled_RecordsNoFailureAndNoRun()
    {
        await _fixture.AddPendingInboxRowAsync(
            MessagingMetricsTestData.CancelledRowId,
            MessagingMetricsTestData.CancellingMessageType,
            MessagingMetricsTestData.CancelledText,
            InboxProcessorTestData.UserActorId,
            InboxProcessorTestData.UserCompanyId,
            InboxTestData.Now);
        using var failed = Collect<long>(Instruments.Failed);
        using var retried = Collect<long>(Instruments.Retried);
        using var runs = Collect<double>(Instruments.RunDuration);
        var processor = _fixture.Create<InboxProcessor>();

        var act = () => processor.RunAsync(_fixture.CancellationTrigger.Source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();

        var failedMeasurements = failed.GetMeasurementSnapshot();
        var retriedMeasurements = retried.GetMeasurementSnapshot();
        var runMeasurements = runs.GetMeasurementSnapshot();

        failedMeasurements.Should().BeEmpty();
        retriedMeasurements.Should().BeEmpty();
        runMeasurements.Should().BeEmpty();
    }

    private MetricCollector<T> Collect<T>(string instrument)
        where T : struct =>
        new(_fixture.MeterFactory, Constants.Meters.Messaging, instrument);
}
