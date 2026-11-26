using HealthyFac.Telemetry.Domain.Evaluation.Phases;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.UnitTests.Evaluation;

[Feature(TestFeatures.Evaluation)]
public sealed class PhaseIntervalLogTests
{
    private static readonly Guid MachineId = Guid.Parse(input: "11111111-1111-1111-1111-111111111111");
    private static readonly Guid Cutting = Guid.Parse(input: "22222222-2222-2222-2222-222222222222");
    private static readonly Guid Stopped = Guid.Parse(input: "33333333-3333-3333-3333-333333333333");

    private static readonly DateTime Start = new(
        year: 2026, month: 1, day: 1, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

    [Fact]
    public void Follow_WithTheFirstSampleOfAMachine_OpensAnIntervalForTheCurrentPhase()
    {
        var log = new PhaseIntervalLog(MachineId, open: null);

        var opened = log.Follow(Cutting, isSwitched: false, Start);

        opened.Should().NotBeNull();
        opened.OperatingPhaseId.Should().Be(Cutting);
        opened.StartedAt.Should().Be(Start);
        opened.EndedAt.Should().BeNull();
        log.Open.Should().BeSameAs(opened);
    }

    [Fact]
    public void Follow_WithASwitchOfThePhase_ClosesTheOpenIntervalAndOpensTheNextAtTheSampleTime()
    {
        var log = new PhaseIntervalLog(MachineId, open: null);
        var first = log.Follow(Cutting, isSwitched: false, Start)!;

        var next = log.Follow(Stopped, isSwitched: true, Start.AddMinutes(value: 5));

        first.EndedAt.Should().Be(Start.AddMinutes(value: 5));
        next.Should().NotBeNull();
        next.OperatingPhaseId.Should().Be(Stopped);
        next.StartedAt.Should().Be(Start.AddMinutes(value: 5));
        log.Open.Should().BeSameAs(next);
    }

    [Fact]
    public void Follow_WithASwitchToTheUnknownMode_OpensAnIntervalWithoutAPhase()
    {
        var log = new PhaseIntervalLog(MachineId, open: null);
        log.Follow(Cutting, isSwitched: false, Start);

        var next = log.Follow(phaseId: null, isSwitched: true, Start.AddMinutes(value: 1));

        next!.OperatingPhaseId.Should().BeNull();
    }

    [Fact]
    public void Follow_WithNoSwitch_KeepsTheOpenIntervalAndOpensNothing()
    {
        var log = new PhaseIntervalLog(MachineId, open: null);
        var first = log.Follow(Cutting, isSwitched: false, Start)!;

        var next = log.Follow(Cutting, isSwitched: false, Start.AddMinutes(value: 1));

        next.Should().BeNull();
        first.EndedAt.Should().BeNull();
        log.Open.Should().BeSameAs(first);
    }

    [Fact]
    public void Close_WhenTheDataStops_ClosesTheOpenIntervalAtTheGivenTimeAndTheNextSampleOpensANewOne()
    {
        var log = new PhaseIntervalLog(MachineId, open: null);
        var first = log.Follow(Cutting, isSwitched: false, Start)!;

        log.Close(Start.AddMinutes(value: 3));
        var afterTheGap = log.Follow(Cutting, isSwitched: false, Start.AddMinutes(value: 60));

        first.EndedAt.Should().Be(Start.AddMinutes(value: 3));
        afterTheGap.Should().NotBeNull();
        afterTheGap.StartedAt.Should().Be(Start.AddMinutes(value: 60));
    }

    [Fact]
    public void Close_WithATimeBeforeTheStart_GivesAnEmptyInterval()
    {
        var log = new PhaseIntervalLog(MachineId, open: null);
        var first = log.Follow(Cutting, isSwitched: false, Start)!;

        log.Close(Start.AddMinutes(value: -1));

        first.EndedAt.Should().Be(Start);
    }

    [Fact]
    public void Close_WithNoOpenInterval_ChangesNothing()
    {
        var log = new PhaseIntervalLog(MachineId, open: null);

        var act = () => log.Close(Start);

        act.Should().NotThrow();
        log.Open.Should().BeNull();
    }
}
