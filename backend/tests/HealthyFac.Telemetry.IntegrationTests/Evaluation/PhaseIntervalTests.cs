using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.SignalEpisodes.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation;

[Collection(TelemetryEvaluationCollection.Name)]
public sealed class PhaseIntervalTests(TelemetryEvaluationFixture fixture)
{
    private readonly TelemetryEvaluationFixture _fixture = fixture;

    [Fact]
    public async Task Evaluate_WithAPhaseSwitch_ClosesTheOpenIntervalAndOpensTheNextAtTheSwitchSample()
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
                new Segment(Count: 40, Mean: 50, [arrangement.CuttingStatusId]),
                new Segment(Count: 40, Mean: 50, [arrangement.PartChangeStatusId])));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 100));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var intervals = await _fixture.LoadPhaseIntervalsAsync(arrangement.MachineId);

        intervals.Select(interval => interval.OperatingPhaseId).Should().Equal(null, cutting, partChange);
        intervals[index: 0].EndedAt.Should().Be(intervals[index: 1].StartedAt);
        intervals[index: 1].EndedAt.Should().Be(intervals[index: 2].StartedAt);
        intervals[index: 2].EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task Evaluate_WithAMachineWithoutPhases_StoresNoInterval()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 50, count: 20, _fixture.Origin));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 100));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var intervals = await _fixture.LoadPhaseIntervalsAsync(arrangement.MachineId);

        intervals.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_WhenTheSilenceRuleOpens_ClosesTheOpenIntervalAtTheLastSample()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "cutting", priority: 0, arrangement.CuttingStatusId, isTracked: true, limits: null);
        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Constant(
                arrangement, value: 50, count: 11, _fixture.Origin, stepSeconds: 1, arrangement.CuttingStatusId));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 300));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var interval = (await _fixture.LoadPhaseIntervalsAsync(arrangement.MachineId)).Single();

        interval.EndedAt.Should().Be(_fixture.At(seconds: 10));
    }

    [Fact]
    public async Task
        Evaluate_WhenTheMachineIsNotOperational_ClosesTheOpenIntervalAtTheWatermarkAndOpensAnotherAfterwards()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "cutting", priority: 0, arrangement.CuttingStatusId, isTracked: true, limits: null);
        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Constant(
                arrangement, value: 50, count: 10, _fixture.Origin, stepSeconds: 1, arrangement.CuttingStatusId));
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 30));
        await _fixture.EvaluateAsync(arrangement.MachineId);
        await _fixture.Database.ChangeMachineStateAsync(
            MachineRegistrationTestData.CreateStateMessage(
                arrangement.MachineId, MachineStateValue.InMaintenance, _fixture.At(seconds: 31)));
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 60));
        await _fixture.EvaluateAsync(arrangement.MachineId);
        await _fixture.Database.ChangeMachineStateAsync(
            MachineRegistrationTestData.CreateStateMessage(
                arrangement.MachineId, MachineStateValue.Operational, _fixture.At(seconds: 61)));
        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Constant(
                arrangement, value: 50, count: 5, _fixture.At(seconds: 70), stepSeconds: 1,
                arrangement.CuttingStatusId));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 100));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var intervals = await _fixture.LoadPhaseIntervalsAsync(arrangement.MachineId);

        intervals.Should().HaveCount(expected: 2);
        intervals[index: 0].EndedAt.Should().Be(_fixture.At(seconds: 9));
        intervals[index: 1].StartedAt.Should().Be(_fixture.At(seconds: 70));
        intervals[index: 1].EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task Evaluate_WhenTheMachineLosesAllItsPhases_ClosesTheOpenIntervalAtTheNextSample()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        var cutting = await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "cutting", priority: 0, arrangement.CuttingStatusId, isTracked: true, limits: null);
        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Constant(
                arrangement, value: 50, count: 10, _fixture.Origin, stepSeconds: 1, arrangement.CuttingStatusId));
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 30));
        await _fixture.EvaluateAsync(arrangement.MachineId);
        await _fixture.SoftDeletePhaseAsync(cutting);
        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Constant(
                arrangement, value: 50, count: 5, _fixture.At(seconds: 40), stepSeconds: 1,
                arrangement.CuttingStatusId));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 60));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var intervals = await _fixture.LoadPhaseIntervalsAsync(arrangement.MachineId);

        intervals.Should().OnlyContain(interval => interval.EndedAt != null);
        intervals.Max(interval => interval.EndedAt).Should().Be(_fixture.At(seconds: 40));
    }
}
