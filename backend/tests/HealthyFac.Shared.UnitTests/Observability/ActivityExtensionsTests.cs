using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.UnitTests.Observability.Data;

namespace HealthyFac.Shared.UnitTests.Observability;

public sealed class ActivityExtensionsTests : IDisposable
{
    private const string TestSourceName = "HealthyFac.Tests.ActivityExtensions";

    private readonly ActivitySource _source = new(TestSourceName);
    private readonly ActivityListener _listener;

    public ActivityExtensionsTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TestSourceName,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
        };

        ActivitySource.AddActivityListener(_listener);
    }

    [Theory]
    [MemberData(nameof(ActivityOutcomeTestData.Rejections), MemberType = typeof(ActivityOutcomeTestData))]
    public void MarkException_WithExpectedRejection_LeavesStatusUnsetAndRecordsNoException(Exception exception)
    {
        using var activity = _source.StartActivity("rejection");

        activity.MarkException(exception);

        activity!.Status.Should().Be(ActivityStatusCode.Unset);
        activity.GetTagItem(ObservabilityConstants.Tags.Outcome).Should().Be(ObservabilityConstants.Outcomes.Rejected);
        activity.Events.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ActivityOutcomeTestData.Failures), MemberType = typeof(ActivityOutcomeTestData))]
    public void MarkException_WithUnexpectedException_SetsErrorStatusAndRecordsException(Exception exception)
    {
        using var activity = _source.StartActivity("failure");

        activity.MarkException(exception);

        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.GetTagItem(ObservabilityConstants.Tags.Outcome).Should().Be(ObservabilityConstants.Outcomes.Failure);
        activity.Events.Should().ContainSingle();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _source.Dispose();
    }
}
