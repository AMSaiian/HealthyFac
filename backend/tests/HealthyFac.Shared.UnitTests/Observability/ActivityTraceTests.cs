using System.Diagnostics;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.UnitTests.Observability.Data;

namespace HealthyFac.Shared.UnitTests.Observability;

public sealed class ActivityTraceTests : IDisposable
{
    private readonly ActivitySource _source = new(ActivityTraceTestData.SourceName);
    private readonly ActivityListener _listener;

    public ActivityTraceTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ActivityTraceTestData.SourceName,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
        };

        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public async Task TraceAsync_WhenWorkSucceeds_MarksSuccessAndReturnsResult()
    {
        using var activity = _source.StartActivity(ActivityTraceTestData.ActivityName);

        var result = await activity.TraceAsync(
            () => Task.FromResult(ActivityTraceTestData.Result),
            CancellationToken.None);

        result.Should().Be(ActivityTraceTestData.Result);
        activity!.Status.Should().Be(ActivityStatusCode.Ok);
        activity.GetTagItem(ObservabilityConstants.Tags.Outcome).Should().Be(ObservabilityConstants.Outcomes.Success);
    }

    [Fact]
    public async Task TraceAsync_WhenWorkThrows_MarksFailureAndRethrows()
    {
        using var activity = _source.StartActivity(ActivityTraceTestData.ActivityName);
        var failure = ActivityTraceTestData.CreateFailure();

        var act = () => activity.TraceAsync(() => Task.FromException(failure), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().Where(exception => exception == failure);
        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.GetTagItem(ObservabilityConstants.Tags.Outcome).Should().Be(ObservabilityConstants.Outcomes.Failure);
    }

    [Fact]
    public async Task TraceAsync_WhenWorkRejects_MarksRejectedAndRethrows()
    {
        using var activity = _source.StartActivity(ActivityTraceTestData.ActivityName);
        var rejection = ActivityTraceTestData.CreateRejection();

        var act = () => activity.TraceAsync(() => Task.FromException(rejection), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().Where(exception => exception == rejection);
        activity!.Status.Should().Be(ActivityStatusCode.Unset);
        activity.GetTagItem(ObservabilityConstants.Tags.Outcome).Should().Be(ObservabilityConstants.Outcomes.Rejected);
    }

    [Fact]
    public async Task TraceAsync_WhenCancelled_LeavesStatusUnsetAndRethrows()
    {
        using var activity = _source.StartActivity(ActivityTraceTestData.ActivityName);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = () => activity.TraceAsync(
            () => Task.FromCanceled(cancellation.Token),
            cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        activity!.Status.Should().Be(ActivityStatusCode.Unset);
        activity.GetTagItem(ObservabilityConstants.Tags.Outcome).Should().BeNull();
    }

    [Fact]
    public async Task TraceAsync_WithNullActivity_RunsWork()
    {
        Activity? activity = null;
        var isWorkRun = false;

        await activity.TraceAsync(
            () =>
            {
                isWorkRun = true;

                return Task.CompletedTask;
            },
            CancellationToken.None);

        isWorkRun.Should().BeTrue();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _source.Dispose();
    }
}
