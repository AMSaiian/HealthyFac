using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.IntegrationTests.Evaluation;
using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.SignalEpisodes.Data;

namespace HealthyFac.Telemetry.IntegrationTests.SignalEpisodes;

[Collection(TelemetryEvaluationCollection.Name)]
public sealed class PhaseTimesReadTests(TelemetryEvaluationFixture fixture)
{
    private const int HourSeconds = 3600;

    private readonly TelemetryEvaluationFixture _fixture = fixture;

    [Fact]
    public async Task GetPhaseTimes_WithIntervalsThatCrossTheRange_ClipsThemAndAddsUpToTheRangeWithUnknownAndNoData()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        var cutting = await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "cutting", priority: 0, arrangement.CuttingStatusId, isTracked: true, limits: null);

        await _fixture.AddPhaseIntervalAsync(
            arrangement.MachineId, cutting, _fixture.At(seconds: -600), _fixture.At(seconds: 600));

        await _fixture.AddPhaseIntervalAsync(
            arrangement.MachineId, phaseId: null, _fixture.At(seconds: 600), _fixture.At(seconds: 900));

        await _fixture.AddPhaseIntervalAsync(
            arrangement.MachineId, cutting, _fixture.At(seconds: 1800), _fixture.At(HourSeconds + 600));

        _fixture.Time.SetUtcNow(_fixture.At(HourSeconds * 2));

        var times = await _fixture.GetPhaseTimesUseCaseAsync(RangeRequest(arrangement.MachineId));

        times.Phases.Single(phase => phase.PhaseId == cutting).Seconds.Should().Be(600 + (HourSeconds - 1800));
        times.UnknownModeSeconds.Should().Be(expected: 300);
        times.NoDataSeconds.Should().Be(expected: 900);

        (times.Phases.Sum(phase => phase.Seconds) + times.UnknownModeSeconds + times.NoDataSeconds)
            .Should()
            .Be(HourSeconds);
    }

    [Fact]
    public async Task GetPhaseTimes_WithADeletedPhaseThatHasTime_KeepsItsNameWithTheDeletedFlag()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        var cutting = await _fixture.AddEvaluationPhaseAsync(
            arrangement, code: "cutting", priority: 0, arrangement.CuttingStatusId, isTracked: true, limits: null);

        await _fixture.AddPhaseIntervalAsync(
            arrangement.MachineId, cutting, _fixture.At(seconds: 0), _fixture.At(seconds: 300));

        await _fixture.SoftDeletePhaseAsync(cutting);
        _fixture.Time.SetUtcNow(_fixture.At(HourSeconds * 2));

        var times = await _fixture.GetPhaseTimesUseCaseAsync(RangeRequest(arrangement.MachineId));

        var phase = times.Phases.Single();
        phase.Name.Should().Be(expected: "cutting");
        phase.IsDeleted.Should().BeTrue();
        phase.Seconds.Should().Be(expected: 300);
    }

    [Fact]
    public async Task GetPhaseTimes_WithAnotherCompany_ThrowsNotFoundException()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        var act = () => _fixture.GetPhaseTimesUseCaseAsync(
            RangeRequest(arrangement.MachineId) with { CompanyId = MachineRegistrationTestData.OtherCompanyId });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetPhaseTimes_WithAnEndBeforeTheStart_ThrowsValidationException()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        var act = () => _fixture.GetPhaseTimesUseCaseAsync(
            RangeRequest(arrangement.MachineId) with { To = _fixture.At(seconds: -1) });

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    private GetPhaseTimesRequest RangeRequest(Guid machineId) =>
        new()
        {
            CompanyId = MachineRegistrationTestData.CompanyId,
            MachineId = machineId,
            From = _fixture.At(seconds: 0),
            To = _fixture.At(HourSeconds),
        };
}
