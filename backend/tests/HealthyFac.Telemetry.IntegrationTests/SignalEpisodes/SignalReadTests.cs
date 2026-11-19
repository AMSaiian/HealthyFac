using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.SignalEpisodes;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using HealthyFac.Telemetry.IntegrationTests.Evaluation;
using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.SignalEpisodes.Data;

namespace HealthyFac.Telemetry.IntegrationTests.SignalEpisodes;

[Collection(TelemetryEvaluationCollection.Name)]
public sealed class SignalReadTests(TelemetryEvaluationFixture fixture)
{
    private readonly TelemetryEvaluationFixture _fixture = fixture;

    [Fact]
    public async Task GetMetricHistory_WithAnHourAnd120Buckets_GroupsTheReadingsIn30SecondBuckets()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await _fixture.SeedSamplesAsync(
            arrangement,
            new[] { 5, 10, 35, 3599 }.Select((second, index) => Sample(arrangement, second, (index + 1) * 10)));

        var response = await _fixture.GetMetricHistoryUseCaseAsync(HistoryRequest(arrangement));

        response.Buckets.Select(bucket => (bucket.Start, bucket.Avg, bucket.Min, bucket.Max, bucket.Count))
            .Should()
            .Equal(
                (_fixture.At(seconds: 0), 15d, 10d, 20d, 2L),
                (_fixture.At(SignalReadTestData.HourBucketSeconds), 30d, 30d, 30d, 1L),
                (_fixture.At(SignalReadTestData.HourSeconds - SignalReadTestData.HourBucketSeconds), 40d, 40d, 40d,
                    1L));
    }

    [Fact]
    public async Task GetMetricHistory_WithAReadingOutsideTheRange_LeavesItOut()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await _fixture.SeedSamplesAsync(
            arrangement,
            [
                Sample(arrangement, second: -1, value: 1),
                Sample(arrangement, second: 0, value: 2),
                Sample(arrangement, SignalReadTestData.HourSeconds, value: 3),
            ]);

        var response = await _fixture.GetMetricHistoryUseCaseAsync(HistoryRequest(arrangement));

        response.Buckets.Should().ContainSingle().Which.Max.Should().Be(expected: 2);
    }

    [Theory]
    [MemberData(nameof(SignalReadTestData.InvalidHistoryRanges), MemberType = typeof(SignalReadTestData))]
    public async Task GetMetricHistory_WithAnInvalidCountOrRange_ThrowsValidationException(
        string caseName,
        int bucketCount,
        int toSeconds
    )
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        var act = () => _fixture.GetMetricHistoryUseCaseAsync(
            HistoryRequest(arrangement) with { BucketCount = bucketCount, To = _fixture.At(toSeconds) });

        await act.Should().ThrowAsync<AppValidationException>(caseName);
    }

    [Fact]
    public async Task GetMetricHistory_WithTheCapAsCount_ReturnsTheBuckets()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await _fixture.SeedSamplesAsync(arrangement, [Sample(arrangement, second: 0, value: 1)]);

        var response = await _fixture.GetMetricHistoryUseCaseAsync(
            HistoryRequest(arrangement) with { BucketCount = SignalReadTestData.HistoryBucketCap });

        response.Buckets.Should().ContainSingle();
    }

    [Fact]
    public async Task GetMetricHistory_WithAnotherCompany_ThrowsNotFoundException()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        var act = () => _fixture.GetMetricHistoryUseCaseAsync(
            HistoryRequest(arrangement) with { CompanyId = MachineRegistrationTestData.OtherCompanyId });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetMetricHistory_WithTheMetricOfAnotherMachine_ThrowsNotFoundException()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        var other = await _fixture.ArrangeEvaluationMachineAsync();

        var act = () => _fixture.GetMetricHistoryUseCaseAsync(
            HistoryRequest(arrangement) with { MetricDefinitionId = other.MetricId });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetSignalEpisodes_WithARange_ReturnsTheOverlappingEpisodesWithTheirSnapshotsInOpenOrder()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await _fixture.AddEpisodeAtAsync(
            arrangement, SignalRule.Threshold, _fixture.At(seconds: 200), resolvedAt: null);
        await _fixture.AddEpisodeAtAsync(
            arrangement, SignalRule.Drift, _fixture.At(seconds: 100), _fixture.At(seconds: 150));
        await _fixture.AddEpisodeAtAsync(
            arrangement, SignalRule.Trend, _fixture.At(seconds: -500), _fixture.At(seconds: -1));
        await _fixture.AddEpisodeAtAsync(arrangement, SignalRule.Status, _fixture.At(seconds: 5000), resolvedAt: null);

        var response = await _fixture.GetSignalEpisodesUseCaseAsync(
            new GetSignalEpisodesRequest
            {
                CompanyId = MachineRegistrationTestData.CompanyId,
                MachineId = arrangement.MachineId,
                From = _fixture.At(seconds: 0),
                To = _fixture.At(SignalReadTestData.HourSeconds),
            });

        response.Items.Select(item => (item.Rule, item.Value, item.Limit))
            .Should()
            .Equal((SignalRule.Drift.Value, 1d, 9d), (SignalRule.Threshold.Value, 1d, 9d));
    }

    [Fact]
    public async Task GetSignalEpisodes_WithAnotherCompany_ThrowsNotFoundException()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        var act = () => _fixture.GetSignalEpisodesUseCaseAsync(
            new GetSignalEpisodesRequest
            {
                CompanyId = MachineRegistrationTestData.OtherCompanyId,
                MachineId = arrangement.MachineId,
                From = _fixture.At(seconds: 0),
                To = _fixture.At(SignalReadTestData.HourSeconds),
            });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private GetMetricHistoryRequest HistoryRequest(EvaluationArrangement arrangement) =>
        new()
        {
            CompanyId = MachineRegistrationTestData.CompanyId,
            MachineId = arrangement.MachineId,
            MetricDefinitionId = arrangement.MetricId,
            From = _fixture.At(seconds: 0),
            To = _fixture.At(SignalReadTestData.HourSeconds),
            BucketCount = SignalReadTestData.HourBucketCount,
        };

    private SeededSample Sample(EvaluationArrangement arrangement, int second, double value) =>
        new(_fixture.At(second), new Dictionary<Guid, double> { [arrangement.MetricId] = value }, []);
}
