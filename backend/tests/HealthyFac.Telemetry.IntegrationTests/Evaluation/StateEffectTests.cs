using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.OperatingPhases.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation;

[Collection(TelemetryEvaluationCollection.Name)]
public sealed class StateEffectTests(TelemetryEvaluationFixture fixture)
{
    private readonly TelemetryEvaluationFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(StateEffectTestData.BlockedStates), MemberType = typeof(StateEffectTestData))]
    public async Task Evaluate_InMaintenanceOrOutOfService_OpensNothingLearnsNothingAndMovesTheWatermark(
        string caseName,
        MachineStateValue state
    )
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await ChangeStateAsync(arrangement.MachineId, state, _fixture.At(seconds: -10));
        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 95, count: 10, _fixture.Origin));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 30));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var evaluation = await _fixture.LoadEvaluationAsync(arrangement.MachineId);
        var episodes = await _fixture.LoadEpisodesAsync(arrangement.MachineId);
        var statistics = await _fixture.LoadStatisticsAsync(arrangement.MetricId);

        episodes.Should().BeEmpty(caseName);
        statistics.Should().BeEmpty(caseName);
        evaluation.Watermark.Should().Be(_fixture.At(seconds: 9), caseName);
    }

    [Theory]
    [MemberData(nameof(StateEffectTestData.BlockedStates), MemberType = typeof(StateEffectTestData))]
    public async Task Evaluate_InMaintenanceOrOutOfService_OpensNoSilentMachineEpisode(
        string caseName,
        MachineStateValue state
    )
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await ChangeStateAsync(arrangement.MachineId, state, _fixture.At(seconds: -10));
        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 50, count: 1, _fixture.Origin));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 500));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episodes = await _fixture.LoadEpisodesAsync(arrangement.MachineId);

        episodes.Should().BeEmpty(caseName);
    }

    [Fact]
    public async Task Evaluate_AfterTheMachineIsOperationalAgain_StartsFromTheMovedWatermark()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await ChangeStateAsync(arrangement.MachineId, MachineStateValue.InMaintenance, _fixture.At(seconds: -10));
        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 95, count: 10, _fixture.Origin));
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 30));
        await _fixture.EvaluateAsync(arrangement.MachineId);
        await ChangeStateAsync(arrangement.MachineId, MachineStateValue.Operational, _fixture.At(seconds: 31));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 60));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episodes = await _fixture.LoadEpisodesAsync(arrangement.MachineId);

        episodes.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(StateEffectTestData.StateRoundTrips), MemberType = typeof(StateEffectTestData))]
    public async Task ChangeState_BackToOperational_ResetsOnlyAfterMaintenance(
        string caseName,
        MachineStateValue state,
        bool isReset
    )
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement,
                _fixture.Origin,
                new Segment(Count: 70, Mean: 50, [], HasWiggle: true),
                new Segment(Count: 5, Mean: 95, [])));
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 90));
        await _fixture.EvaluateAsync(arrangement.MachineId);
        await ChangeStateAsync(arrangement.MachineId, state, _fixture.At(seconds: 100));

        await ChangeStateAsync(arrangement.MachineId, MachineStateValue.Operational, _fixture.At(seconds: 200));

        var statistics = await _fixture.LoadStatisticsAsync(arrangement.MetricId);
        var episodes = await _fixture.LoadEpisodesAsync(arrangement.MachineId);
        var messages = await _fixture.LoadEpisodeMessagesAsync(arrangement.MachineId);

        statistics.Any().Should().Be(!isReset, caseName);
        episodes.Single().State.Should().Be(isReset ? SignalEpisodeState.Resolved : SignalEpisodeState.Open, caseName);
        episodes.Single().ResolvedAt.Should().Be(isReset ? _fixture.At(seconds: 200) : (DateTime?)null, caseName);
        messages.Last().State.Should().Be(isReset ? "resolved" : "open", caseName);
    }

    [Theory]
    [MemberData(nameof(StateEffectTestData.PhaseChanges), MemberType = typeof(StateEffectTestData))]
    public async Task UpdatePhase_WithAChange_RestartsTheStatisticsOnlyWhenTheMeaningChanged(
        string caseName,
        Func<UpdateOperatingPhaseRequest, EvaluationArrangement, UpdateOperatingPhaseRequest> change,
        bool isRestarted
    )
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        var phaseId = await LearnInPhaseAsync(arrangement);

        await _fixture.Database.UpdatePhaseUseCaseAsync(
            change(
                StateEffectTestData.UpdateRequest(arrangement, phaseId, MachineRegistrationTestData.CompanyId),
                arrangement));

        var statistics = await _fixture.LoadStatisticsAsync(arrangement.MetricId);

        statistics.Any().Should().Be(!isRestarted, caseName);
    }

    [Fact]
    public async Task DeletePhase_RemovesTheStatisticsOfThePhase()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();
        var phaseId = await LearnInPhaseAsync(arrangement);

        await _fixture.Database.DeletePhaseUseCaseAsync(
            MachineRegistrationTestData.CompanyId, arrangement.MachineId, phaseId);

        var statistics = await _fixture.LoadStatisticsAsync(arrangement.MetricId);

        statistics.Should().BeEmpty();
    }

    private async Task<Guid> LearnInPhaseAsync(EvaluationArrangement arrangement)
    {
        var phaseId = await _fixture.AddEvaluationPhaseAsync(
            arrangement,
            StateEffectTestData.PhaseCode,
            priority: 0,
            arrangement.CuttingStatusId,
            isTracked: true,
            limits: null);
        await _fixture.SeedSamplesAsync(
            arrangement,
            EvaluationTestData.Build(
                arrangement,
                _fixture.Origin,
                new Segment(Count: 150, Mean: 50, [arrangement.CuttingStatusId], HasWiggle: true)));
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 170));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        (await _fixture.LoadStatisticsAsync(arrangement.MetricId)).Should()
            .ContainSingle()
            .Which.OperatingPhaseId.Should()
            .Be(phaseId);

        return phaseId;
    }

    private Task ChangeStateAsync(Guid machineId, MachineStateValue state, DateTime startedAt) =>
        _fixture.Database.ChangeMachineStateAsync(
            MachineRegistrationTestData.CreateStateMessage(machineId, state, startedAt));
}
