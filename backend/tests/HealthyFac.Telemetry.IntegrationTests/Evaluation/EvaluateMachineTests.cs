using System.Diagnostics.Metrics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation;

[Collection(TelemetryEvaluationCollection.Name)]
[Feature(TestFeatures.Evaluation)]
public sealed class EvaluateMachineTests(TelemetryEvaluationFixture fixture)
{
    private const double Tolerance = 1e-9;

    private readonly TelemetryEvaluationFixture _fixture = fixture;

    [Fact]
    public async Task Evaluate_WithSamplesAroundTheSettleLimit_ReadsOnlyThoseBeforeItInOrder()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 50, count: 10, _fixture.Origin));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 9));
        await _fixture.EvaluateAsync(arrangement.MachineId);
        var first = await _fixture.LoadEvaluationAsync(arrangement.MachineId);
        var firstRows = await _fixture.LoadStatisticsAsync(arrangement.MetricId);

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 20));
        await _fixture.EvaluateAsync(arrangement.MachineId);
        var second = await _fixture.LoadEvaluationAsync(arrangement.MachineId);
        var secondRows = await _fixture.LoadStatisticsAsync(arrangement.MetricId);

        first.Watermark.Should().Be(_fixture.At(seconds: 4));
        firstRows.Should().ContainSingle().Which.LastSampledAt.Should().Be(_fixture.At(seconds: 4));
        second.Watermark.Should().Be(_fixture.At(seconds: 9));
        second.DueAt.Should().Be(_fixture.At(seconds: 30));
        secondRows.Should().ContainSingle().Which.SamplesSinceRestart.Should().Be(expected: 10);
        secondRows[0].OperatingPhaseId.Should().BeNull();
    }

    [Fact]
    public async Task Evaluate_WithABacklogInThreeRuns_GivesTheSameResultAsOneBigRun()
    {
        var split = await _fixture.ArrangeEvaluationMachineAsync();
        var whole = await _fixture.ArrangeEvaluationMachineAsync();
        await _fixture.SeedSamplesAsync(split, ExcursionSeries(split, _fixture.Origin));
        await _fixture.SeedSamplesAsync(whole, ExcursionSeries(whole, _fixture.Origin));

        foreach (var seconds in new[] { 45, 90, 200 })
        {
            _fixture.Time.SetUtcNow(_fixture.At(seconds));
            await _fixture.EvaluateAsync(split.MachineId);
        }

        await _fixture.EvaluateAsync(whole.MachineId);

        var splitRow = (await _fixture.LoadStatisticsAsync(split.MetricId)).Single();
        var wholeRow = (await _fixture.LoadStatisticsAsync(whole.MetricId)).Single();
        var splitEpisodes = await _fixture.LoadEpisodesAsync(split.MachineId);
        var wholeEpisodes = await _fixture.LoadEpisodesAsync(whole.MachineId);

        splitRow.WarmMean.Should().BeApproximately(wholeRow.WarmMean, Tolerance);
        splitRow.WarmM2.Should().BeApproximately(wholeRow.WarmM2, Tolerance);
        splitRow.SlowMean.Should().BeApproximately(wholeRow.SlowMean, Tolerance);
        splitRow.Variance.Should().BeApproximately(wholeRow.Variance, Tolerance);
        splitRow.FastMean.Should().BeApproximately(wholeRow.FastMean, Tolerance);
        splitRow.Level.Should().BeApproximately(wholeRow.Level, Tolerance);
        splitRow.DriftSpread.Should().BeApproximately(wholeRow.DriftSpread, Tolerance);
        splitRow.SamplesSinceRestart.Should().Be(wholeRow.SamplesSinceRestart);
        Summarize(splitEpisodes).Should().Equal(Summarize(wholeEpisodes));
    }

    [Fact]
    public async Task Evaluate_AfterACatchUp_StoresTheEpisodeTimesOfTheSamplesAndOneMessageForEachChange()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await _fixture.SeedSamplesAsync(arrangement, ExcursionSeries(arrangement, _fixture.Origin));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 200));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episode = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();
        var messages = await _fixture.LoadEpisodeMessagesAsync(arrangement.MachineId);

        episode.Rule.Should().Be(SignalRule.Threshold);
        episode.OpenedAt.Should().Be(_fixture.At(seconds: 61));
        episode.EscalatedAt.Should().Be(_fixture.At(seconds: 66));
        episode.ResolvedAt.Should().Be(_fixture.At(seconds: 71));
        messages.Select(message => message.Sequence).Should().Equal(1, 2, 3);
        messages.Select(message => message.State).Should().Equal("open", "open", "resolved");
        messages.Select(message => message.Severity).Should().Equal("warning", "alarm", "alarm");

        messages.Select(message => message.ChangedAt)
            .Should()
            .Equal(
                _fixture.At(seconds: 61), _fixture.At(seconds: 66), _fixture.At(seconds: 71));

        messages.Should().OnlyContain(message => message.MetricKey == "spindle.temperature");
    }

    [Fact]
    public async Task Evaluate_WithALateReading_ChangesNothingKeepsItStoredAndCountsIt()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Series(arrangement, EvaluationTestData.Wiggle(mean: 50, count: 70), _fixture.Origin));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 80));
        await _fixture.EvaluateAsync(arrangement.MachineId);
        var before = (await _fixture.LoadStatisticsAsync(arrangement.MetricId)).Single();

        using var collector = new MetricCollector<long>(
            _fixture.Database.GetSingleton<IMeterFactory>(),
            ObservabilityConstants.Evaluation.Meter,
            ObservabilityConstants.Evaluation.Instruments.LateReadings);

        await _fixture.SeedSamplesAsync(
            arrangement,
            [
                new SeededSample(
                    _fixture.At(seconds: 30.5),
                    new Dictionary<Guid, double> { [arrangement.MetricId] = 100 },
                    [],
                    _fixture.At(seconds: 81)),
            ]);

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 90));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var after = (await _fixture.LoadStatisticsAsync(arrangement.MetricId)).Single();
        var evaluation = await _fixture.LoadEvaluationAsync(arrangement.MachineId);
        var episodes = await _fixture.LoadEpisodesAsync(arrangement.MachineId);

        after.SamplesSinceRestart.Should().Be(before.SamplesSinceRestart);
        after.SlowMean.Should().Be(before.SlowMean);
        evaluation.Watermark.Should().Be(_fixture.At(seconds: 69));
        episodes.Should().BeEmpty();
        collector.GetMeasurementSnapshot().Sum(measurement => measurement.Value).Should().Be(expected: 1);
    }

    [Fact]
    public async Task Evaluate_WithThresholdAndStatusSamplesBeforeWarmUp_OpensThemAtOnce()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        var active = new[] { arrangement.WarningStatusId, arrangement.ErrorStatusId };

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(arrangement, _fixture.Origin, new Segment(Count: 5, Mean: 95, active)));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 30));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episodes = await _fixture.LoadEpisodesAsync(arrangement.MachineId);
        var statistics = (await _fixture.LoadStatisticsAsync(arrangement.MetricId)).Single();

        statistics.IsWarm.Should().BeFalse();

        Summarize(episodes)
            .Should()
            .BeEquivalentTo(
                Line(rule: "threshold", severity: "alarm", state: "open", _fixture.At(seconds: 1)) + "   1",
                Line(rule: "status", severity: "warning", state: "open", _fixture.At(seconds: 0)) + "   1",
                Line(rule: "status", severity: "alarm", state: "open", _fixture.At(seconds: 0)) + "   1");
    }

    [Fact]
    public async Task Evaluate_InAnUntrackedPhase_OpensTheThresholdAndStoresNoStatistics()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "cutting", priority: 0, arrangement.CuttingStatusId, isTracked: false, limits: null);

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement, _fixture.Origin, new Segment(Count: 100, Mean: 95, [arrangement.CuttingStatusId])));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 200));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episodes = await _fixture.LoadEpisodesAsync(arrangement.MachineId);
        var statistics = await _fixture.LoadStatisticsAsync(arrangement.MetricId);

        episodes.Should().ContainSingle().Which.Rule.Should().Be(SignalRule.Threshold);
        statistics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_WithTrackedPhases_LearnsEachPhaseInItsOwnRow()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        var cutting = await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "cutting", priority: 0, arrangement.CuttingStatusId, isTracked: true, limits: null);

        var partChange = await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "part.change", priority: 1, arrangement.PartChangeStatusId, isTracked: true,
            limits: null);

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement,
                _fixture.Origin,
                new Segment(Count: 150, Mean: 50, [arrangement.CuttingStatusId], HasWiggle: true),
                new Segment(Count: 150, Mean: 70, [arrangement.PartChangeStatusId], HasWiggle: true),
                new Segment(Count: 150, Mean: 50, [arrangement.CuttingStatusId], HasWiggle: true)));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 500));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var rows = await _fixture.LoadStatisticsAsync(arrangement.MetricId);

        rows.Select(row => row.OperatingPhaseId).Should().BeEquivalentTo([cutting, partChange]);

        rows.Single(row => row.OperatingPhaseId == cutting)
            .WarmMean.Should()
            .BeApproximately(expectedValue: 50, precision: 0.1);

        rows.Single(row => row.OperatingPhaseId == partChange)
            .WarmMean.Should()
            .BeApproximately(expectedValue: 70, precision: 0.1);
    }

    [Fact]
    public async Task Evaluate_WhileNothingMatches_NeverLearnsAndOpensUnknownModeAfterTheDwell()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "cutting", priority: 0, arrangement.CuttingStatusId, isTracked: true, limits: null);

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement,
                _fixture.Origin,
                new Segment(Count: 100, Mean: 50, [], HasWiggle: true),
                new Segment(Count: 20, Mean: 50, [arrangement.CuttingStatusId], HasWiggle: true)));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 200));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episode = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();
        var statistics = await _fixture.LoadStatisticsAsync(arrangement.MetricId);

        episode.Rule.Should().Be(SignalRule.UnknownMode);
        episode.OpenedAt.Should().Be(_fixture.At(seconds: 61));
        episode.ResolvedAt.Should().Be(_fixture.At(seconds: 100));
        statistics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_AfterAPhaseSwitchThatChangesTheLimits_WaitsForTheSettleTimeBeforeCounting()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.AddEvaluationPhaseAsync(
            arrangement,
            code: "cutting",
            priority: 0,
            arrangement.CuttingStatusId,
            isTracked: true,
            new MetricLimits { AlarmHigh = 120 });

        await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "part.change", priority: 1, arrangement.PartChangeStatusId, isTracked: true,
            limits: null);

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement,
                _fixture.Origin,
                new Segment(Count: 40, Mean: 50, [arrangement.CuttingStatusId]),
                new Segment(Count: 70, Mean: 95, [arrangement.PartChangeStatusId]),
                new Segment(Count: 80, Mean: 95, [arrangement.CuttingStatusId])));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 300));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episode = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();

        episode.Rule.Should().Be(SignalRule.Threshold);
        episode.OpenedAt.Should().Be(_fixture.At(seconds: 86));
        episode.ResolvedAt.Should().Be(_fixture.At(seconds: 156));
    }

    [Fact]
    public async Task Evaluate_InAPhaseWithAnEmptyLimitRow_OpensNothingForAValueThatTheDefinitionLimitsWouldReject()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync(new MetricLimits { AlarmLow = 10 });

        await _fixture.AddEvaluationPhaseAsync(
            arrangement,
            code: "stopped",
            priority: 0,
            arrangement.PartChangeStatusId,
            isTracked: true,
            new MetricLimits());

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement,
                _fixture.Origin,
                new Segment(Count: 30, Mean: 50, [arrangement.PartChangeStatusId]),
                new Segment(Count: 70, Mean: 0, [arrangement.PartChangeStatusId])));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 200));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episodes = await _fixture.LoadEpisodesAsync(arrangement.MachineId);

        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_InAPhaseWithoutALimitRow_UsesTheDefinitionLimits()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync(new MetricLimits { AlarmLow = 10 });

        await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "stopped", priority: 0, arrangement.PartChangeStatusId, isTracked: true, limits: null);

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement,
                _fixture.Origin,
                new Segment(Count: 30, Mean: 50, [arrangement.PartChangeStatusId]),
                new Segment(Count: 70, Mean: 0, [arrangement.PartChangeStatusId])));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 200));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episode = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();

        episode.Rule.Should().Be(SignalRule.Threshold);
        episode.Severity.Should().Be(SignalSeverity.Alarm);
    }

    [Fact]
    public async Task Evaluate_InAPhaseWithItsOwnAlarmLimit_OpensTheAlarmForAValueAboveIt()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync(new MetricLimits());

        await _fixture.AddEvaluationPhaseAsync(
            arrangement,
            code: "stopped",
            priority: 0,
            arrangement.PartChangeStatusId,
            isTracked: true,
            new MetricLimits { AlarmHigh = 90 });

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement,
                _fixture.Origin,
                new Segment(Count: 30, Mean: 50, [arrangement.PartChangeStatusId]),
                new Segment(Count: 70, Mean: 95, [arrangement.PartChangeStatusId])));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 200));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episode = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();

        episode.Severity.Should().Be(SignalSeverity.Alarm);
        episode.Snapshot.PhaseCode.Should().Be(expected: "stopped");
    }

    [Fact]
    public async Task Evaluate_WhileAnEpisodeIsOpen_FreezesTheBaseline()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement, _fixture.Origin, new Segment(Count: 70, Mean: 50, [], HasWiggle: true)));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 80));
        await _fixture.EvaluateAsync(arrangement.MachineId);
        var before = (await _fixture.LoadStatisticsAsync(arrangement.MetricId)).Single();

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(arrangement, _fixture.At(seconds: 70), new Segment(Count: 30, Mean: 95, [])));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 200));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var after = (await _fixture.LoadStatisticsAsync(arrangement.MetricId)).Single();

        after.SlowMean.Should().BeApproximately(before.SlowMean, precision: 0.05);
        after.FastMean.Should().BeGreaterThan(before.FastMean + 10);
    }

    [Fact]
    public async Task Evaluate_WhenThePumpStopsWithFlowZero_OpensNothingBeforeDuringOrAfterTheSwitch()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync(new MetricLimits());

        await _fixture.AddEvaluationPhaseAsync(
            arrangement,
            code: "running",
            priority: 0,
            arrangement.CuttingStatusId,
            isTracked: true,
            new MetricLimits { AlarmLow = 10 });

        await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "stopped", priority: 1, arrangement.PartChangeStatusId, isTracked: true,
            new MetricLimits());

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement,
                _fixture.Origin,
                new Segment(Count: 60, Mean: 50, [arrangement.CuttingStatusId]),
                new Segment(Count: 70, Mean: 0, [arrangement.PartChangeStatusId])));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 200));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episodes = await _fixture.LoadEpisodesAsync(arrangement.MachineId);

        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_WithAValueBeyondTheAlarmLimitOfBothPhases_OpensDuringTheDwell()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync(new MetricLimits());

        await _fixture.AddEvaluationPhaseAsync(
            arrangement,
            code: "running",
            priority: 0,
            arrangement.CuttingStatusId,
            isTracked: true,
            new MetricLimits { AlarmHigh = 90 });

        await _fixture.AddEvaluationPhaseAsync(
            arrangement,
            code: "stopped",
            priority: 1,
            arrangement.PartChangeStatusId,
            isTracked: true,
            new MetricLimits { AlarmHigh = 95 });

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement,
                _fixture.Origin,
                new Segment(Count: 60, Mean: 50, [arrangement.CuttingStatusId]),
                new Segment(Count: 10, Mean: 98, [arrangement.PartChangeStatusId])));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 100));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episode = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();

        episode.Severity.Should().Be(SignalSeverity.Alarm);
        episode.OpenedAt.Should().Be(_fixture.At(seconds: 61));
    }

    [Fact]
    public async Task Evaluate_WhileAnEpisodeIsOpenInTheDwell_DoesNotResolveInsideTheLooserLimitsOnly()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync(new MetricLimits());

        await _fixture.AddEvaluationPhaseAsync(
            arrangement,
            code: "running",
            priority: 0,
            arrangement.CuttingStatusId,
            isTracked: true,
            new MetricLimits { AlarmHigh = 90 });

        await _fixture.AddEvaluationPhaseAsync(
            arrangement,
            code: "stopped",
            priority: 1,
            arrangement.PartChangeStatusId,
            isTracked: true,
            new MetricLimits { AlarmHigh = 120 });

        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement,
                _fixture.Origin,
                new Segment(Count: 40, Mean: 50, [arrangement.CuttingStatusId]),
                new Segment(Count: 20, Mean: 95, [arrangement.CuttingStatusId]),
                new Segment(Count: 10, Mean: 95, [arrangement.PartChangeStatusId])));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 100));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episode = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();

        episode.State.Should().Be(SignalEpisodeState.Open);
        episode.OpenedAt.Should().Be(_fixture.At(seconds: 46));
    }

    private static List<SeededSample> ExcursionSeries(EvaluationArrangement arrangement, DateTime start) =>
        EvaluationTestData.Build(
            arrangement,
            start,
            new Segment(Count: 60, Mean: 50, [], HasWiggle: true),
            new Segment(Count: 5, Mean: 85, []),
            new Segment(Count: 5, Mean: 95, []),
            new Segment(Count: 40, Mean: 50, [], HasWiggle: true));

    private static List<string> Summarize(IEnumerable<SignalEpisode> episodes) =>
    [
        .. episodes
            .Select(episode =>
                Line(episode.Rule.Value, episode.Severity.Value, episode.State.Value, episode.OpenedAt)
                + $" {episode.EscalatedAt:O} {episode.ResolvedAt:O} {episode.Sequence}"),
    ];

    private static string Line(string rule, string severity, string state, DateTime openedAt) =>
        $"{rule} {severity} {state} {openedAt:O}";
}
