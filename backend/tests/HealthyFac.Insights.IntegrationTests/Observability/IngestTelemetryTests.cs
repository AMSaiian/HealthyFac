using System.Diagnostics;
using System.Diagnostics.Metrics;
using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Infrastructure.Ingest;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Ingest.Data;
using HealthyFac.Testing.Tracing;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Constants = HealthyFac.Shared.Application.Observability.ObservabilityConstants;

namespace HealthyFac.Insights.IntegrationTests.Observability;

[Collection(InsightsCollection.Name)]
public sealed class IngestTelemetryTests(InsightsFixture fixture)
{
    private const string StartSourceName = "HealthyFac.Insights.Tests.Start";
    private static readonly string[] GenAiContentTags =
    [
        "gen_ai.input.messages",
        "gen_ai.output.messages",
        "gen_ai.system_instructions",
    ];

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task Run_Completed_WritesTheProcessSpanWithLinkChildrenAndTags()
    {
        using var capture = new SpanCapture(Constants.Ingest.Source, Constants.Ingest.Ai);
        _fixture.ArrangeFile();
        var startTraceId = await StartInsideATraceAsync();

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var process = capture.Spans.Single(span => span.OperationName == Constants.Ingest.Spans.Process);
        var convert = capture.Spans.Single(span => span.OperationName == Constants.Ingest.Spans.Convert);
        var store = capture.Spans.Single(span => span.OperationName == Constants.Ingest.Spans.Store);

        process.Kind.Should().Be(ActivityKind.Internal);
        process.ParentId.Should().BeNull();
        process.Links.Should().ContainSingle().Which.Context.TraceId.Should().Be(startTraceId);
        process.GetTagItem(Constants.Ingest.Tags.Result).Should().Be(Constants.Ingest.Results.Completed);
        process.GetTagItem(Constants.Ingest.Tags.FileType).Should().Be("pdf");
        process.GetTagItem(Constants.Ingest.Tags.ErrorCode).Should().BeNull();
        process.Status.Should().NotBe(ActivityStatusCode.Error);
        convert.ParentId.Should().Be(process.Id);
        store.ParentId.Should().Be(process.Id);
    }

    [Fact]
    public async Task Run_Failed_SetsTheErrorStatusAndTheErrorCode()
    {
        using var capture = new SpanCapture(Constants.Ingest.Source);
        _fixture.ArrangeFile();
        await _fixture.StartAttemptAsync();
        _fixture.Ingest.Converter.Behavior = _ => throw new ConversionFailedException("Unsupported layout");

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var process = capture.Spans.Single(span => span.OperationName == Constants.Ingest.Spans.Process);

        process.Status.Should().Be(ActivityStatusCode.Error);
        process.GetTagItem(Constants.Ingest.Tags.Result).Should().Be(Constants.Ingest.Results.Failed);
        process.GetTagItem(Constants.Ingest.Tags.ErrorCode).Should().Be("conversionFailed");
    }

    [Fact]
    public async Task Run_Cancelled_WritesTheInterruptReason()
    {
        using var capture = new SpanCapture(Constants.Ingest.Source);
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.Ingest.Converter.Behavior = async _ =>
        {
            await _fixture.CancelAttemptAsync(attemptId);

            return FakeDocumentConverter.DefaultDocument;
        };

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var process = capture.Spans.Single(span => span.OperationName == Constants.Ingest.Spans.Process);

        process.GetTagItem(Constants.Ingest.Tags.Result).Should().Be(Constants.Ingest.Results.Interrupted);
        process.GetTagItem(Constants.Ingest.Tags.InterruptReason).Should().Be("cancelled");
    }

    [Fact]
    public async Task Run_Completed_WritesNoPromptOrAnswerTextToTheModelSpans()
    {
        using var capture = new SpanCapture(Constants.Ingest.Ai);
        _fixture.ArrangeFile();
        await _fixture.StartAttemptAsync();

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var modelSpans = capture.Spans;
        var contentTags = modelSpans
            .SelectMany(span => span.TagObjects)
            .Select(tag => tag.Key)
            .Where(GenAiContentTags.Contains)
            .ToList();

        modelSpans.Should().NotBeEmpty();
        contentTags.Should().BeEmpty();
    }

    [Fact]
    public async Task Run_Completed_RecordsAttemptsStagesAndCountsWithUnits()
    {
        var attempts = Collect<long>(Constants.Ingest.Instruments.Attempts);
        var durations = Collect<double>(Constants.Ingest.Instruments.Duration);
        var pages = Collect<long>(Constants.Ingest.Instruments.Pages);
        var chunks = Collect<long>(Constants.Ingest.Instruments.Chunks);
        var drafts = Collect<long>(Constants.Ingest.Instruments.Drafts);
        _fixture.ArrangeFile();
        await _fixture.StartAttemptAsync();

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var stages = durations.GetMeasurementSnapshot()
            .Select(measurement => measurement.Tags[Constants.Ingest.Tags.Stage])
            .ToList();
        var draftKinds = drafts.GetMeasurementSnapshot()
            .ToDictionary(
                measurement => (string)measurement.Tags[Constants.Ingest.Tags.DraftKind]!,
                measurement => measurement.Value);

        var attempt = attempts.GetMeasurementSnapshot().Should().ContainSingle().Which;
        attempt.Tags[Constants.Ingest.Tags.Result].Should().Be(Constants.Ingest.Results.Completed);
        attempts.Instrument!.Unit.Should().Be("{attempt}");
        durations.Instrument!.Unit.Should().Be("s");
        stages.Should().BeEquivalentTo(
            new[]
            {
                Constants.Ingest.Stages.Convert,
                Constants.Ingest.Stages.Embed,
                Constants.Ingest.Stages.Extract,
                Constants.Ingest.Stages.Store,
            });
        pages.LastMeasurement!.Value.Should().Be(expected: 2);
        pages.Instrument!.Unit.Should().Be("{page}");
        chunks.LastMeasurement!.Value.Should().Be(expected: 2);
        chunks.Instrument!.Unit.Should().Be("{chunk}");
        draftKinds.Should().BeEquivalentTo(
            new Dictionary<string, long>
            {
                [Constants.Ingest.DraftKinds.Machine] = 1,
                [Constants.Ingest.DraftKinds.Metric] = 1,
                [Constants.Ingest.DraftKinds.Status] = 1,
            });
        drafts.Instrument!.Unit.Should().Be("{draft}");
    }

    [Fact]
    public async Task Run_Failed_TagsTheAttemptCounterWithTheErrorCode()
    {
        var attempts = Collect<long>(Constants.Ingest.Instruments.Attempts);
        _fixture.ArrangeFile();
        await _fixture.StartAttemptAsync();
        _fixture.Ingest.Converter.Behavior = _ => throw new ConversionFailedException("Unsupported layout");

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var attempt = attempts.GetMeasurementSnapshot().Should().ContainSingle().Which;
        attempt.Tags[Constants.Ingest.Tags.Result].Should().Be(Constants.Ingest.Results.Failed);
        attempt.Tags[Constants.Ingest.Tags.ErrorCode].Should().Be("conversionFailed");
    }

    [Fact]
    public async Task Sample_WithQueuedAttempts_ReportsThePendingCountAndTheOldestAge()
    {
        var pending = Collect<long>(Constants.Ingest.Instruments.Pending);
        var age = Collect<double>(Constants.Ingest.Instruments.OldestPendingAge);
        _fixture.ArrangeFile();
        await _fixture.StartAttemptAsync();

        await _fixture.GetSingleton<IngestBacklogSampler>().SampleAsync(TestContext.Current.CancellationToken);
        pending.RecordObservableInstruments();
        age.RecordObservableInstruments();

        pending.LastMeasurement!.Value.Should().Be(expected: 1);
        pending.Instrument!.Unit.Should().Be("{attempt}");
        age.LastMeasurement!.Value.Should().BeGreaterThanOrEqualTo(0);
        age.Instrument!.Unit.Should().Be("s");
    }

    private MetricCollector<T> Collect<T>(string instrument)
        where T : struct =>
        new(_fixture.GetSingleton<IMeterFactory>(), Constants.Ingest.Source, instrument);

    private async Task<ActivityTraceId> StartInsideATraceAsync()
    {
        using var source = new ActivitySource(StartSourceName);
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => candidate.Name == StartSourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        using var start = source.StartActivity("Start");

        await _fixture.StartAttemptAsync();

        return start!.TraceId;
    }
}
