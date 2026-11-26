using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using HealthyFac.Telemetry.Messaging.Signals;
using HealthyFac.Testing.Features;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.IntegrationTests.Cases;

[Collection(SignalCaseCollection.Name)]
[Feature(TestFeatures.Cases)]
[Feature(TestFeatures.Evaluation)]
public sealed class RecordSignalEpisodeTests(SignalCaseFixture fixture)
{
    private readonly SignalCaseFixture _fixture = fixture;

    [Fact]
    public async Task Receive_ThroughTheInbox_OpensTheCaseAndMarksTheRowProcessed()
    {
        var outcome = await _fixture.ReceiveSignalThroughInboxAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));

        var cases = await _fixture.LoadCasesAsync();
        var signals = await _fixture.LoadSignalsAsync();
        outcome.IsProcessed.Should().BeTrue(outcome.Error);
        cases.Should().ContainSingle();
        signals.Should().ContainSingle().Which.CaseId.Should().Be(cases[0].Id);
    }

    [Fact]
    public async Task Record_WithARepeatOrAnOlderSequence_ChangesNothing()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, severity: SignalSeverityValue.Alarm));

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, severity: SignalSeverityValue.Warning));

        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));

        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        signal.Severity.Should().Be(SignalSeverity.Alarm);
        signal.Sequence.Should().Be(expected: 2);
    }

    [Fact]
    public async Task Record_WithAHigherSequence_AppliesTheMessage()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, severity: SignalSeverityValue.Alarm));

        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        signal.Severity.Should().Be(SignalSeverity.Alarm);
    }

    [Fact]
    public async Task Record_WithTheFirstEpisode_OpensACaseWithANewSignal()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));

        var cases = await _fixture.LoadCasesAsync();
        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);

        var openCase = cases.Should().ContainSingle().Subject;
        openCase.State.Should().Be(CaseState.Open);
        openCase.MachineId.Should().Be(CaseTestData.MachineId);
        signal.CaseId.Should().Be(openCase.Id);
        signal.Coverage.Should().Be(SignalCoverage.New);
    }

    [Fact]
    public async Task Record_WithASecondEpisodeOfTheMachine_JoinsTheOpenCase()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));

        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.SecondEpisodeId, sequence: 1));

        var cases = await _fixture.LoadCasesAsync();
        var signals = await _fixture.LoadSignalsAsync();
        cases.Should().ContainSingle();
        signals.Select(signal => signal.CaseId).Should().OnlyContain(caseId => caseId == cases[0].Id);
    }

    [Fact]
    public async Task Record_WithAnEpisodeOfAnotherMachine_OpensASeparateCase()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(
                CaseTestData.SecondEpisodeId, sequence: 1, machineId: CaseTestData.OtherMachineId));

        var cases = await _fixture.LoadCasesAsync();

        cases.Select(candidate => candidate.MachineId)
            .Should()
            .BeEquivalentTo([CaseTestData.MachineId, CaseTestData.OtherMachineId]);
    }

    [Fact]
    public async Task Record_WithThreeChangesInTheDebounce_QueuesOneTriageRun()
    {
        var start = _fixture.Time.GetUtcNow().UtcDateTime;
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, severity: SignalSeverityValue.Alarm));

        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.SecondEpisodeId, sequence: 1));

        var runs = await _fixture.LoadRunsAsync();
        var cases = await _fixture.LoadCasesAsync();

        var run = runs.Should().ContainSingle().Subject;
        run.Role.Should().Be(AgentRole.Triage);
        run.State.Should().Be(AgentRunState.Queued);
        run.CaseId.Should().Be(cases[0].Id);
        run.DueAt.Should().Be(start.AddSeconds(CaseTestData.DebounceSeconds));
    }

    [Fact]
    public async Task Record_WithAResolve_QueuesNoRun()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(
                CaseTestData.FirstEpisodeId, sequence: 2, SignalEpisodeStateValue.Resolved));

        var runs = await _fixture.LoadRunsAsync();
        runs.Should().BeEmpty();
    }

    [Fact]
    public async Task Record_WithAnEscalationOfADraftedSignal_QueuesNoRun()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.DraftSignalAsync(CaseTestData.FirstEpisodeId);
        await _fixture.ClearRunsAsync();

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, severity: SignalSeverityValue.Alarm));

        var runs = await _fixture.LoadRunsAsync();
        runs.Should().BeEmpty();
    }

    [Fact]
    public async Task Record_WithAnEscalationOfAnUncoveredSignal_QueuesARun()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, severity: SignalSeverityValue.Alarm));

        var runs = await _fixture.LoadRunsAsync();
        runs.Should().ContainSingle();
    }

    [Fact]
    public async Task Record_WithEveryEpisodeResolved_ResolvesTheCaseAndTheNextEpisodeOpensANewCase()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.SecondEpisodeId, sequence: 1));

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(
                CaseTestData.FirstEpisodeId, sequence: 2, SignalEpisodeStateValue.Resolved));

        var casesWhileOneIsOpen = await _fixture.LoadCasesAsync();

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(
                CaseTestData.SecondEpisodeId, sequence: 2, SignalEpisodeStateValue.Resolved));

        var casesAfterTheResolve = await _fixture.LoadCasesAsync();
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.ThirdEpisodeId, sequence: 1));

        var casesAfterTheNextEpisode = await _fixture.LoadCasesAsync();
        casesWhileOneIsOpen.Single().State.Should().Be(CaseState.Open);
        casesAfterTheResolve.Single().State.Should().Be(CaseState.Resolved);

        casesAfterTheNextEpisode.Select(candidate => candidate.State)
            .Should()
            .BeEquivalentTo([CaseState.Resolved, CaseState.Open]);
    }

    [Fact]
    public async Task Record_WithAnEscalationOfADismissedSignal_MakesItNewAndMovesItToTheOpenCase()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.DismissSignalAsync(CaseTestData.FirstEpisodeId);
        var firstCase = (await _fixture.LoadCasesAsync()).Single();
        await _fixture.CloseCaseAsync(firstCase.Id);
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.SecondEpisodeId, sequence: 1));
        var secondCase = (await _fixture.LoadCasesAsync()).Single(candidate => candidate.State == CaseState.Open);

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, severity: SignalSeverityValue.Alarm));

        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        signal.Coverage.Should().Be(SignalCoverage.New);
        signal.CaseId.Should().Be(secondCase.Id);
    }

    [Fact]
    public async Task Record_WithAnEscalationOfADismissedSignalAndNoOpenCase_OpensANewCaseForIt()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        var firstCase = (await _fixture.LoadCasesAsync()).Single();
        await _fixture.CloseCaseAsync(firstCase.Id);

        await _fixture.HandleSignalAsync(
            CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 2, severity: SignalSeverityValue.Alarm));

        var cases = await _fixture.LoadCasesAsync();
        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        cases.Should().HaveCount(expected: 2);
        signal.CaseId.Should().Be(cases.Single(candidate => candidate.State == CaseState.Open).Id);
    }

    [Fact]
    public async Task Record_WithANewCase_LeavesADismissedSignalOfTheMachineOutOfIt()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        var firstCase = (await _fixture.LoadCasesAsync()).Single();
        await _fixture.CloseCaseAsync(firstCase.Id);

        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.SecondEpisodeId, sequence: 1));

        var dismissed = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var cases = await _fixture.LoadCasesAsync();
        dismissed.Coverage.Should().Be(SignalCoverage.Dismissed);
        dismissed.CaseId.Should().Be(firstCase.Id);
        cases.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task Record_WithANewCase_TakesOverTheOpenDraftedSignalOfAClosedCase()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.DraftSignalAsync(CaseTestData.FirstEpisodeId);
        var firstCase = (await _fixture.LoadCasesAsync()).Single();
        await _fixture.CloseCaseAsync(firstCase.Id);

        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.SecondEpisodeId, sequence: 1));

        var drafted = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var newCase = (await _fixture.LoadCasesAsync()).Single(candidate => candidate.State == CaseState.Open);
        drafted.CaseId.Should().Be(newCase.Id);
        drafted.Coverage.Should().Be(SignalCoverage.Drafted);
    }

    [Fact]
    public async Task Record_WhenAJoinOverlapsTheResolveOfTheCase_FailsOneSaveOnTheCaseRowVersion()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        using var race = _fixture.Inner.SaveGate.StartRace(CaseTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.HandleSignalAsync(
                CaseTestData.Message(
                    CaseTestData.FirstEpisodeId, sequence: 2, SignalEpisodeStateValue.Resolved))),
            race.RunAsync(() =>
                _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.SecondEpisodeId, sequence: 1))));

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is DbUpdateConcurrencyException).Should().Be(expected: 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
    }

    [Fact]
    public async Task Record_WhenTwoResolvesOfTheCaseOverlap_FailsOneSaveSoTheCaseCannotStayOpen()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.SecondEpisodeId, sequence: 1));
        using var race = _fixture.Inner.SaveGate.StartRace(CaseTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.HandleSignalAsync(
                CaseTestData.Message(
                    CaseTestData.FirstEpisodeId, sequence: 2, SignalEpisodeStateValue.Resolved))),
            race.RunAsync(() => _fixture.HandleSignalAsync(
                CaseTestData.Message(
                    CaseTestData.SecondEpisodeId, sequence: 2, SignalEpisodeStateValue.Resolved))));

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is DbUpdateConcurrencyException).Should().Be(expected: 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
    }
}
